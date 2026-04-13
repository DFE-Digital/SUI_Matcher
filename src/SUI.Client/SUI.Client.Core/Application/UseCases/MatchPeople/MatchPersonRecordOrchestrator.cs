using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public sealed class MatchPersonRecordOrchestrator<TSource>(
    ILogger<MatchPersonRecordOrchestrator<TSource>> logger,
    IMatchingApiClient matchingApiClient,
    IPersonSpecParser<TSource> personSpecParser,
    IOptions<PersonMatchingOptions> options
) : IMatchPersonRecordOrchestrator<TSource>
{
    public async Task<List<ProcessedMatchRecord<TSource>>> ProcessAsync(
        IEnumerable<TSource> content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var stats = new MatchingProcessStats();
        var processedBatch = new List<ProcessedMatchRecord<TSource>>();
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
                    new ProcessedMatchRecord<TSource>
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
                    new ProcessedMatchRecord<TSource>
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
