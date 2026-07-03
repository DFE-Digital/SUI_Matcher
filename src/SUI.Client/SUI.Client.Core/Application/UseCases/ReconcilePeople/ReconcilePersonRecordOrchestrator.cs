using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public sealed class ReconcilePersonRecordOrchestrator<TSource>(
    ILogger<ReconcilePersonRecordOrchestrator<TSource>> logger,
    IMatchingApiClient matchingApiClient,
    IPersonSpecParser<TSource> personSpecParser,
    IReconciliationDataParser<TSource> reconciliationDataParser,
    AddressComparisonOrchestrator addressComparisonOrchestrator,
    IOptions<PersonMatchingOptions> options,
    IOptions<OptionalPropertiesLog> optionalPropertiesLogOptions
) : IMatchPersonRecordOrchestrator<TSource>
{
    public async Task<List<ProcessedMatchRecord<TSource>>> ProcessAsync(
        IEnumerable<TSource> content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var processedBatch = new List<ProcessedMatchRecord<TSource>>();

        foreach (var record in content)
        {
            try
            {
                var person = personSpecParser.Parse(record);
                var sourceData = reconciliationDataParser.Parse(record);
                var payload = new ReconciliationRequest
                {
                    NhsNumber = sourceData.NhsNumber,
                    Given = person.Given,
                    Family = person.Family,
                    BirthDate = person.BirthDate,
                    RawBirthDate = person.RawBirthDate,
                    Gender = person.Gender,
                    Phone = person.Phone,
                    Email = person.Email,
                    AddressPostalCode = person.AddressPostalCode,
                    OptionalProperties = GetLoggableOptionalProperties(person.OptionalProperties),
                    SearchStrategy = options.Value.SearchStrategy,
                    StrategyVersion = options.Value.StrategyVersion,
                };

                var response = await matchingApiClient.ReconcilePersonAsync(
                    payload,
                    cancellationToken
                );
                var matchingResponse = response is null
                    ? null
                    : new PersonMatchResponse
                    {
                        Result = response.MatchingResult,
                        SearchId = response.SearchId,
                    };
                var addressComparison = addressComparisonOrchestrator.GetAddressComparisonResult(
                    payload,
                    response,
                    sourceData.AddressHistory
                );

                processedBatch.Add(
                    new ProcessedMatchRecord<TSource>
                    {
                        OriginalData = record,
                        ApiResult = matchingResponse,
                        ReconciliationResult = response,
                        SourceBirthDate = person.BirthDate,
                        SourceNhsNumber = sourceData.NhsNumber,
                        AddressComparisonResults = addressComparison,
                        IsSuccess =
                            response is not null && response.Status != ReconciliationStatus.Error,
                        ErrorMessage = string.Empty,
                    }
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                processedBatch.Add(
                    new ProcessedMatchRecord<TSource>
                    {
                        OriginalData = record,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                    }
                );
                logger.LogWarning(ex, "Failed to process record in file {FileName}.", fileName);
            }
        }

        return processedBatch;
    }

    private Dictionary<string, object> GetLoggableOptionalProperties(
        Dictionary<string, object> optionalProperties
    )
    {
        if (optionalProperties.Count == 0 || optionalPropertiesLogOptions.Value.Fields.Count == 0)
        {
            return new Dictionary<string, object>();
        }

        var allowedFields = new HashSet<string>(
            optionalPropertiesLogOptions.Value.Fields.Keys,
            StringComparer.OrdinalIgnoreCase
        );

        return optionalProperties
            .Where(optionalProperty => allowedFields.Contains(optionalProperty.Key))
            .ToDictionary(
                optionalProperty => optionalProperty.Key,
                optionalProperty => optionalProperty.Value
            );
    }
}
