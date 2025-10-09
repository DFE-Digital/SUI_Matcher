using System.Diagnostics;

using MatchingApi.Search;

using Newtonsoft.Json;

using Shared;
using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;
using Shared.Util;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MatchingApi.Services;

public class MatchingService(
    ILogger<MatchingService> logger,
    INhsFhirClient nhsFhirClient,
    IValidationService validationService,
    IAuditLogger auditLogger) : IMatchingService
{
    public async Task<PersonMatchResponse> SearchAsync(SearchSpecification searchSpecification, bool logMatch = true)
    {
        var searchId = HashUtil.StoreUniqueSearchIdFor(searchSpecification);
        var searchStrategy = SearchStrategyFactory.Get(searchSpecification.SearchStrategy);
        StoreAlgorithmVersion(searchStrategy.GetAlgorithmVersion(), searchSpecification.SearchStrategy);

        var auditDetails = new Dictionary<string, string>
        {
            { "SearchId", searchId }
        };
        await auditLogger.LogAsync(new AuditLogEntry(AuditLogEntry.AuditLogAction.Match, auditDetails));

        var validationResults = validationService.Validate(searchSpecification);

        var dataQualityResult = DataQualityEvaluatorService.ToQualityResult(searchSpecification, validationResults.Results!.ToList());

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

        var result = await MatchAsync(searchSpecification, searchStrategy);
        if (logMatch)
        {
            LogMatchCompletion(searchSpecification, result.Status, dataQualityResult, result.Score ?? 0,
                result.ProcessStage);
        }

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

    public async Task<PersonMatchResponse> SearchNoLogicAsync(PersonSpecificationForNoLogic personSpecification)
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

    private void StoreAlgorithmVersion(int versionNumber, string searchStrategy)
    {
        Activity.Current?.SetBaggage("AlgorithmVersion", versionNumber.ToString());
        Activity.Current?.SetBaggage(SharedConstants.SearchStrategy.LogName, searchStrategy);
        logger.LogInformation("StoreAlgorithmVersion: Version: {Version}, Strategy {Strategy}", versionNumber, searchStrategy);
    }

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



    private void LogMatchCompletion(
        PersonSpecification personSpecification,
        MatchStatus matchStatus,
        DataQualityResult dataQualityResult,
        decimal score,
        string? resultProcessStage)
    {
        var ageGroup = personSpecification.BirthDate.HasValue
            ? PersonSpecificationUtils.GetAgeGroup(personSpecification.BirthDate.Value)
            : "Unknown";

        logger.LogInformation(
            "[MATCH_COMPLETED] [ConfidenceScore={Score}] [ProcessStage={Stage}] MatchStatus: {MatchStatus}, AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, DataQuality: {DataQuality}, OptionalProperties: {OptionalProperties}",
            score,
            resultProcessStage,
            matchStatus,
            ageGroup,
            personSpecification.Gender ?? "Unknown",
            personSpecification.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(dataQualityResult.ToDictionary()),
            JsonSerializer.Serialize(personSpecification.OptionalProperties)
        );
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

    private async Task<MatchResult2> MatchNoLogicAsync(PersonSpecificationForNoLogic model)
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
            FuzzyMatch = model.FuzzyMatch,
            ExactMatch = model.ExactMatch
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

    private async Task<MatchResult2> MatchAsync(SearchSpecification model, ISearchStrategy strategy)
    {
        var queries = strategy.BuildQuery(model);
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