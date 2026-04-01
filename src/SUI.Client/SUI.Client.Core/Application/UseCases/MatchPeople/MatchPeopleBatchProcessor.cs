using Microsoft.Extensions.Logging;
using Shared.Models;
using SUI.Client.Core.Infrastructure.Http;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public sealed class MatchPeopleBatchProcessor(
    ILogger<MatchPeopleBatchProcessor> logger,
    IMatchingApiClient matchingApiClient
) : IMatchPeopleBatchProcessor
{
    public async Task<MatchingProcessStats> ProcessAsync(
        ProcessPersonBatchRequest request,
        CancellationToken cancellationToken
    )
    {
        // Guard check as it's part of a core lib
        ArgumentNullException.ThrowIfNull(request);

        var stats = new MatchingProcessStats();

        for (var index = 0; index < request.People.Count; index++)
        {
            var person = request.People[index];
            try
            {
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
                    SearchStrategy = request.SearchStrategy,
                    StrategyVersion = request.StrategyVersion,
                };

                var response = await matchingApiClient.MatchPersonAsync(payload, cancellationToken);
                stats.RecordStats(response);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stats.RecordError();
                logger.LogError(
                    ex,
                    "Failed to process row {RowNumber} in batch {BatchIdentifier}. Postcode={Postcode}",
                    index + 1,
                    request.BatchIdentifier ?? "unknown",
                    person.AddressPostalCode
                );
            }
        }

        return stats;
    }
}
