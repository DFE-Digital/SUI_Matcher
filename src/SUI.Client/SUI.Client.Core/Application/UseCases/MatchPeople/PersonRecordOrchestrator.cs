using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public class ProcessedRecord<TSource>
{
    // The untouched original record (Dictionary or GraphType)
    public TSource OriginalData { get; set; } = default!;

    // The result from the API
    public PersonMatchResponse? ApiResult { get; set; }

    // Metadata for the edges to know how to handle this row
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PersonRecordOrchestrator<TSource>(
    ILogger<PersonRecordOrchestrator<TSource>> logger,
    IMatchingApiClient matchingApiClient,
    IPersonSpecParser<TSource> personSpecParser,
    IOptions<PersonMatchingOptions> options
) : IPersonRecordOrchestrator<TSource>
{
    public async Task<List<ProcessedRecord<TSource>>> ProcessAsync(
        IEnumerable<TSource> content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var stats = new MatchingProcessStats();
        var processedBatch = new List<ProcessedRecord<TSource>>();
        foreach (var record in content)
        {
            try
            {
                var person = personSpecParser.Parse(record);

                var payload = new SearchSpecification
                {
                    Given = person.Given,
                    Family = person.Family,
                    BirthDate = person.BirthDate,
                    RawBirthDate = person.RawBirthDate,
                    Gender = person.Gender,
                    Phone = person.Phone,
                    Email = person.Email,
                    AddressPostalCode = person.AddressPostalCode,
                    OptionalProperties = person.OptionalProperties,
                    SearchStrategy = options.Value.SearchStrategy,
                    StrategyVersion = options.Value.StrategyVersion,
                };

                var response = await matchingApiClient.MatchPersonAsync(payload, cancellationToken);
                stats.RecordStats(response);
                processedBatch.Add(
                    new ProcessedRecord<TSource>
                    {
                        OriginalData = record,
                        ApiResult = response,
                        IsSuccess = response?.Result is not null,
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
                stats.RecordError();
                processedBatch.Add(
                    new ProcessedRecord<TSource>
                    {
                        OriginalData = record,
                        ApiResult = null,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                    }
                );
                logger.LogWarning(ex, "Failed to process record in file {FileName}.", fileName);
            }
        }

        return processedBatch;
    }
}
