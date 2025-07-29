using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

namespace MatchingApi.Services;

public class MatchingService(
    ILogger<MatchingService> logger,
    INhsFhirClient nhsFhirClient,
    IValidationService validationService,
    IAuditLogger auditLogger) : IMatchingService
{
    public static readonly int AlgorithmVersion = 2;

    public async Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification)
    {

        StoreAlgorithmVersion();

        var searchId = StoreUniqueSearchIdFor(personSpecification);
        var auditDetails = new Dictionary<string, string>
        {
            { "SearchId", searchId }
        };
        await auditLogger.LogAsync(new AuditLogEntry(AuditLogEntry.AuditLogAction.Match, auditDetails));

        var validationResults = validationService.Validate(personSpecification);

        var dataQualityResult = DataQualityEvaluatorService.ToQualityResult(personSpecification, validationResults.Results!.ToList());


        logger.LogInformation("Person data validation resulted in: {QualityResult}",
            JsonConvert.SerializeObject(dataQualityResult.ToDictionary()));

        if (!HasMinDataRequirements(dataQualityResult))
        {
            logger.LogError("The minimized data requirements for a search weren't met, returning match status 'Error'");

            return new PersonMatchResponse
            {
                Result = new MatchResult { MatchStatus = MatchStatus.Error, },
                DataQuality = dataQualityResult
            };
        }

        var result = await MatchAsync(personSpecification);

        LogMatchCompletion(personSpecification, result.Status, result.Score ?? 0, result.ProcessStage);

        logger.LogInformation("The person match request resulted in match status '{Status}' " +
                              "and confidence score '{Score}' " +
                              "at process stage ({ProcessStage}), and the data quality was " +
                              "{QualityResult}",
            result.Status.ToString(),
            result.Score,
            result.ProcessStage,
            JsonConvert.SerializeObject(dataQualityResult.ToDictionary()));

        return new PersonMatchResponse
        {
            Result = new MatchResult
            {
                MatchStatus = result.Status,
                Score = result.Result?.Score,
                NhsNumber = result.Result?.NhsNumber,
                ProcessStage = result.ProcessStage,
            },
            DataQuality = dataQualityResult
        };
    }

    private static void StoreAlgorithmVersion() =>
        Activity.Current?.SetBaggage("AlgorithmVersion", AlgorithmVersion.ToString());

    public async Task<DemographicResponse?> GetDemographicsAsync(DemographicRequest request)
    {
        logger.LogInformation("Searching for matching person by NHS number");
        await auditLogger.LogAsync(new AuditLogEntry(AuditLogEntry.AuditLogAction.Demographic, null));

        var validationResults = validationService.Validate(request);

        if (validationResults.Results?.Any() == true)
        {
            return new DemographicResponse
            {
                Errors = validationResults.Results.Select(r => r.ErrorMessage ?? "").ToList()
            };
        }

        var result = await nhsFhirClient.PerformSearchByNhsId(request.NhsNumber!);
        return new DemographicResponse()
        {
            Result = result.Result,
            Errors = result.ErrorMessage is null ? [] : [result.ErrorMessage]
        };
    }

    private static string GetAgeGroup(DateOnly birthDate)
    {
        var dateOnlyNow = DateOnly.FromDateTime(DateTime.Now);
        var age = dateOnlyNow.Year - birthDate.Year;
        if (dateOnlyNow.DayOfYear < birthDate.DayOfYear)
        {
            age--;
        }

        return age switch
        {
            < 1 => "Less than 1 year",
            <= 3 => "1-3 years",
            <= 7 => "4-7 years",
            <= 11 => "8-11 years",
            <= 15 => "12-15 years",
            <= 18 => "16-18 years",
            _ => "Over 18 years"
        };
    }

    private void LogMatchCompletion(PersonSpecification personSpecification, MatchStatus matchStatus, decimal score,
        int? resultProcessStage)
    {
        var ageGroup = personSpecification.BirthDate.HasValue
            ? GetAgeGroup(personSpecification.BirthDate.Value)
            : "Unknown";

        logger.LogInformation(
            "[MATCH_COMPLETED] [ConfidenceScore={Score}] [ProcessStage={Stage}] MatchStatus: {MatchStatus}, AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}",
            score,
            resultProcessStage,
            matchStatus,
            ageGroup,
            personSpecification.Gender ?? "Unknown",
            personSpecification.AddressPostalCode ?? "Unknown"
        );
    }

    private static string StoreUniqueSearchIdFor(PersonSpecification personSpecification)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(personSpecification));
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetBaggage("SearchId", hash);

        return hash;
    }

    private static SearchQuery[] GetSearchQueries(PersonSpecification model)
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        var dobRange = new[]
        {
            "ge" + model.BirthDate.Value.AddMonths(-6).ToString("yyyy-MM-dd"),
            "le" + model.BirthDate.Value.AddMonths(6).ToString("yyyy-MM-dd")
        };
        var dob = new[] { "eq" + model.BirthDate.Value.ToString("yyyy-MM-dd") };

        var modelName = model.Given is not null ? new[] { model.Given } : null;
        var queries = new List<SearchQuery>
        {
            new() // exact search on only given, family and dob
            {
                ExactMatch = true,
                Given = modelName,
                Family = model.Family,
                Birthdate = dob
            },
            new() // 1. exact search
            {
                ExactMatch = true,
                Given = modelName,
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = dob,
                AddressPostalcode = model.AddressPostalCode,
            },
            new() // 2. fuzzy search on only given, family and dob
            {
                FuzzyMatch = true,
                Given = modelName,
                Family = model.Family,
                Birthdate = dob
            },
            new() // 3. fuzzy search with given name, family name and DOB.
            {
                FuzzyMatch = true,
                Given = modelName,
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = dob,
                AddressPostalcode = model.AddressPostalCode,
            },
            new() // 4. fuzzy search with given name, family name and DOB range 6 months either side of given date.
            {
                FuzzyMatch = true, Given = modelName, Family = model.Family, Birthdate = dobRange,
            },
        };

        // Only applicable if dob day is less than or equal to 12
        if (model.BirthDate.Value.Day <=
            12) // 5. fuzzy search with given name, family name and DOB. Day swapped with month if day equal to or less than 12.
        {
            var altDob = new DateTime(
                model.BirthDate.Value.Year,
                model.BirthDate.Value.Day,
                model.BirthDate.Value.Month,
                0, 0, 0,
                DateTimeKind.Unspecified
            );

            queries.Add(new SearchQuery
            {
                FuzzyMatch = true,
                Given = modelName,
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = [$"eq{altDob:yyyy-MM-dd}"],
                AddressPostalcode = model.AddressPostalCode,
            });
        }

        return [.. queries];
    }

    private static bool HasMinDataRequirements(DataQualityResult dataQualityResult)
    {
        return dataQualityResult is
        {
            Given: QualityType.Valid,
            Family: QualityType.Valid,
            BirthDate: QualityType.Valid
        };
    }

    private async Task<MatchResult2> MatchAsync(PersonSpecification model)
    {
        var queries = GetSearchQueries(model);

        for (var i = 0; i < queries.Length; i++)
        {
            var query = queries[i];

            logger.LogInformation("Performing search query ({Query}) against Nhs Fhir API", i);

            var searchResult = await nhsFhirClient.PerformSearch(query);
            if (searchResult != null)
            {
                if (searchResult.Type == SearchResult.ResultType.Matched)
                {
                    var status = MatchStatus.NoMatch;
                    var score = searchResult.Score.GetValueOrDefault();
                    if (score >= 0.95m)
                    {
                        status = MatchStatus.Match;
                    }
                    else if (score >= 0.85m)
                    {
                        status = MatchStatus.PotentialMatch;
                    }

                    logger.LogInformation("Search query ({Query}) resulted in status '{Status}' and confidence score '{Score}'", i, status.ToString(), score);

                    return new MatchResult2(searchResult, status, i); // single match with confidence score
                }
                else if (searchResult.Type == SearchResult.ResultType.MultiMatched)
                {
                    logger.LogInformation("Search query ({Query}) resulted in status 'ManyMatch'", i);

                    return new MatchResult2(searchResult, MatchStatus.ManyMatch, i); // multiple matches
                }
            }
        }

        logger.LogInformation("Search query ({QueryLength}) resulted in status 'NoMatch'", queries.Length - 1);

        return new MatchResult2(MatchStatus.NoMatch);
    }
}