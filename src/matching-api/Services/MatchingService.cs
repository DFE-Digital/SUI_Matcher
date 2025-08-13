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
    public static readonly int AlgorithmVersion = 3;
    private const string? DateFormat = "yyyy-MM-dd";

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

        if (!HasMinDataRequirements(dataQualityResult))
        {
            logger.LogInformation("Person data validation resulted in: {QualityResult}",
                JsonConvert.SerializeObject(dataQualityResult.ToDictionary()));

            logger.LogError("The minimized data requirements for a search weren't met, returning match status 'Error'");

            return new PersonMatchResponse
            {
                Result = new MatchResult { MatchStatus = MatchStatus.Error, },
                DataQuality = dataQualityResult
            };
        }

        var result = await MatchAsync(personSpecification);

        LogMatchCompletion(personSpecification, result.Status, dataQualityResult, result.Score ?? 0, result.ProcessStage);

        logger.LogInformation("The person match request resulted in match status '{Status}' " +
                              "and confidence score '{Score}' " +
                              "at process stage ({ProcessStage}), and the data quality was " +
                              "{QualityResult}",
            result.Status.ToString(),
            result.Score ?? 0,
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

    public async Task<PersonMatchResponse> SearchNoLogicAsync(PersonSpecification personSpecification)
    {
        var result = await MatchNoLogicAsync(personSpecification);
        return new PersonMatchResponse
        {
            Result = new MatchResult
            {
                MatchStatus = result.Status,
                Score = result.Result?.Score,
                ProcessStage = result.ProcessStage,
            },
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

    private void LogMatchCompletion(
        PersonSpecification personSpecification,
        MatchStatus matchStatus,
        DataQualityResult dataQualityResult,
        decimal score,
        string? resultProcessStage)
    {
        var ageGroup = personSpecification.BirthDate.HasValue
            ? GetAgeGroup(personSpecification.BirthDate.Value)
            : "Unknown";

        logger.LogInformation(
            "[MATCH_COMPLETED] [ConfidenceScore={Score}] [ProcessStage={Stage}] MatchStatus: {MatchStatus}, AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, DataQuality: {DataQuality}",
            score,
            resultProcessStage,
            matchStatus,
            ageGroup,
            personSpecification.Gender ?? "Unknown",
            personSpecification.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(dataQualityResult.ToDictionary())
        );
    }

    private static string StoreUniqueSearchIdFor(PersonSpecification personSpecification)
    {
        var data = $"{personSpecification.Given}{personSpecification.Family}" +
                   $"{personSpecification.BirthDate}{personSpecification.Gender}{personSpecification.AddressPostalCode}";

        byte[] bytes = Encoding.ASCII.GetBytes(data);
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

    private static OrderedDictionary<string, SearchQuery> GetSearchQueries(PersonSpecification model)
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        var dobRange = new[]
        {
            "ge" + model.BirthDate.Value.AddMonths(-6).ToString(DateFormat),
            "le" + model.BirthDate.Value.AddMonths(6).ToString(DateFormat)
        };
        var dob = new[] { "eq" + model.BirthDate.Value.ToString(DateFormat) };

        var modelName = model.Given is not null ? new[] { model.Given } : null;
        var queryOrderedMap = new OrderedDictionary<string, SearchQuery>
        {
            {
                "ExactGFD", new() // exact search on only given, family and dob
                {
                    ExactMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Birthdate = dob
                }
            },
            {
                "ExactAll", new() // 1. exact search
                {
                    ExactMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Email = model.Email,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Birthdate = dob,
                    AddressPostalcode = model.AddressPostalCode,
                }
            },
            {
                "FuzzyGFD", new() // 2. fuzzy search on only given, family and dob
                {
                    FuzzyMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Birthdate = dob
                }
            },
            {
                "FuzzyAll", new() // 3. fuzzy search with given name, family name and DOB.
                {
                    FuzzyMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Email = model.Email,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Birthdate = dob,
                    AddressPostalcode = model.AddressPostalCode,
                }
            },
            {
                "FuzzyGFDRange", new() // 4. fuzzy search with given name, family name and DOB range 6 months either side of given date.
                {
                    FuzzyMatch = true, Given = modelName, Family = model.Family, Birthdate = dobRange,
                }
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

            queryOrderedMap.Add("FuzzyAltDob", new SearchQuery
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

        return queryOrderedMap;
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

    private async Task<MatchResult2> MatchNoLogicAsync(PersonSpecification model)
    {
        if (model.RawBirthDate?.Length == 0)
        {
            throw new InvalidOperationException("RawBirthdate is required for search queries");
        }

        var modelName = model.Given is not null ? new[] { model.Given } : null;

        var query = new SearchQuery
        {
            AddressPostalcode = model.AddressPostalCode,
            Gender = model.Gender,
            Email = model.Email,
            Given = modelName,
            Family = model.Family,
            Phone = model.Phone,
            Birthdate = model.RawBirthDate,
            FuzzyMatch = false,
            ExactMatch = false,
        };

        var matchStatus = MatchStatus.Error;
        var searchResult = await nhsFhirClient.PerformSearch(query);

        if (searchResult == null)
        {
            return new MatchResult2(MatchStatus.Error);
        }

        logger.LogInformation(
            "Search query ({Query}) resulted in status '{Status}' and confidence score '{Score}'",
            "SimpleQuery", searchResult.Type, searchResult.Score);

        switch (searchResult.Type)
        {
            case SearchResult.ResultType.Matched:
                matchStatus = MatchStatus.Match;
                break;
            case SearchResult.ResultType.MultiMatched:
                matchStatus = MatchStatus.ManyMatch;
                break;
            case SearchResult.ResultType.Unmatched:
                matchStatus = MatchStatus.NoMatch;
                break;
        }

        return new MatchResult2(searchResult, matchStatus, searchResult.Score.GetValueOrDefault(), String.Empty);
    }

    private async Task<MatchResult2> MatchAsync(PersonSpecification model)
    {
        var queries = GetSearchQueries(model);

        var bestQueryResult = new BestQueryResult();

        foreach (var queryEntry in queries)
        {
            var queryCode = queryEntry.Key;
            var query = queryEntry.Value;

            logger.LogInformation("Performing search query ({Query}) against Nhs Fhir API", queryCode);

            var searchResult = await nhsFhirClient.PerformSearch(query);
            if (searchResult != null)
            {
                if (searchResult.Type == SearchResult.ResultType.Matched)
                {
                    HandleSingleMatchResult(searchResult, bestQueryResult, queryCode,
                        out MatchStatus status, out decimal score);

                    if (score >= 0.95m)
                    {
                        logger.LogInformation(
                            "Search query ({Query}) resulted in status '{Status}' and confidence score '{Score}'",
                            queryCode, status.ToString(), score);

                        return new MatchResult2(searchResult, status, score, queryCode); // single match with confidence score
                    }
                }

                HandleMultipleMatchesResult(searchResult, bestQueryResult, queryCode);
            }
        }

        if (bestQueryResult.CurrentSearchResult != null)
        {
            logger.LogInformation("Search query ({Query}) resulted in status '{Status}'", bestQueryResult.CurrentQueryCode, bestQueryResult.CurrentStatus);

            return new MatchResult2(bestQueryResult.CurrentSearchResult, bestQueryResult.CurrentStatus, bestQueryResult.CurrentScore, bestQueryResult.CurrentQueryCode);
        }

        logger.LogInformation("Search algorithm resulted in status 'NoMatch'");

        return new MatchResult2(MatchStatus.NoMatch);
    }

    private static void HandleSingleMatchResult(SearchResult searchResult, BestQueryResult bestQueryResult, string queryCode, out MatchStatus status, out decimal score)
    {
        status = MatchStatus.NoMatch;
        score = searchResult.Score.GetValueOrDefault();
        if (score >= 0.95m)
        {
            status = MatchStatus.Match;
        }
        else if (score >= 0.85m)
        {
            status = MatchStatus.PotentialMatch;
        }

        if (score > bestQueryResult.CurrentScore)
        {
            bestQueryResult.CurrentStatus = status;
            bestQueryResult.CurrentScore = score;
            bestQueryResult.CurrentQueryCode = queryCode;
            bestQueryResult.CurrentSearchResult = searchResult;
        }
    }

    private void HandleMultipleMatchesResult(SearchResult searchResult, BestQueryResult bestQueryResult, string queryCode)
    {
        if (searchResult.Type == SearchResult.ResultType.MultiMatched)
        {
            logger.LogInformation("Search query ({Query}) resulted in status 'ManyMatch'", queryCode);

            if (bestQueryResult.CurrentScore == 0 && (int)MatchStatus.ManyMatch <= (int)bestQueryResult.CurrentStatus)
            {
                bestQueryResult.CurrentStatus = MatchStatus.ManyMatch;
                bestQueryResult.CurrentQueryCode = queryCode;
                bestQueryResult.CurrentSearchResult = searchResult;
            }
        }
    }

    private sealed class BestQueryResult
    {
        public MatchStatus CurrentStatus { get; set; } = MatchStatus.NoMatch;
        public decimal CurrentScore { get; set; } = 0;
        public string CurrentQueryCode { get; set; } = "";
        public SearchResult? CurrentSearchResult { get; set; }
    }
}