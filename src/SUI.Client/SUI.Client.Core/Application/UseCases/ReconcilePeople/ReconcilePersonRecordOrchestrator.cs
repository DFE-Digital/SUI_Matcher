using System.Text.Json;
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
    private const string OptionalPropertiesEventName = "RECONCILIATION_OPTIONAL_PROPERTIES";
    private const string OptionalPropertyPrefix = "Optional_";
    private const string OptionalPropertiesPresent = "Present";
    private const string OptionalPropertiesNone = "None";
    private const string OptionalPropertiesNoneMessage = "No optional properties";
    private static readonly EventId OptionalPropertiesLoggedEvent = new(
        1001,
        OptionalPropertiesEventName
    );

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
                var loggableOptionalProperties = GetLoggableOptionalProperties(
                    person.OptionalProperties
                );
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
                    OptionalProperties = new Dictionary<string, object>(),
                    SearchStrategy = options.Value.SearchStrategy,
                    StrategyVersion = options.Value.StrategyVersion,
                };

                var response = await matchingApiClient.ReconcilePersonAsync(
                    payload,
                    cancellationToken
                );

                LogOptionalProperties(response?.SearchId, loggableOptionalProperties);

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
                LogAddressComparisonResult(response?.SearchId, addressComparison);

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

    private void LogOptionalProperties(
        string? searchId,
        Dictionary<string, object> optionalProperties
    )
    {
        using var optionalPropertiesScope = BeginOptionalPropertiesScope(optionalProperties);

        logger.LogInformation(
            OptionalPropertiesLoggedEvent,
            "[{EventName}] SearchId: {SearchId}, OptionalPropertiesStatus: {OptionalPropertiesStatus}, OptionalPropertiesCount: {OptionalPropertiesCount}, OptionalProperties: {OptionalProperties}",
            OptionalPropertiesEventName,
            searchId ?? "Unknown",
            optionalProperties.Count == 0 ? OptionalPropertiesNone : OptionalPropertiesPresent,
            optionalProperties.Count,
            FormatOptionalProperties(optionalProperties)
        );
    }

    private IDisposable? BeginOptionalPropertiesScope(Dictionary<string, object> optionalProperties)
    {
        if (optionalProperties.Count == 0)
        {
            return null;
        }

        return logger.BeginScope(
            optionalProperties.ToDictionary(
                optionalProperty => $"{OptionalPropertyPrefix}{optionalProperty.Key}",
                optionalProperty => (object?)optionalProperty.Value.ToString()
            )
        );
    }

    private static string FormatOptionalProperties(Dictionary<string, object> optionalProperties) =>
        optionalProperties.Count == 0
            ? OptionalPropertiesNoneMessage
            : JsonSerializer.Serialize(optionalProperties);

    private void LogAddressComparisonResult(
        string? searchId,
        AddressComparisonResults addressComparison
    )
    {
        logger.LogInformation(
            "[ADDRESS_COMPARISON_COMPLETED] SearchId: {SearchId}, PrimaryAddressSame: {PrimaryAddressSame}, AddressHistoriesIntersect: {AddressHistoriesIntersect}, PrimarySourceAddressInPDSHistory: {PrimarySourceAddressInPDSHistory}, PrimaryPDSAddressInSourceHistory: {PrimaryPDSAddressInSourceHistory}",
            searchId ?? "Unknown",
            addressComparison.PrimaryAddressSame.GetResultMessage(),
            addressComparison.AddressHistoriesIntersect.GetResultMessage(),
            addressComparison.PrimaryCMSAddressInPDSHistory.GetResultMessage(),
            addressComparison.PrimaryPDSAddressInCMSHistory.GetResultMessage()
        );
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
            optionalPropertiesLogOptions.Value.Fields,
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
