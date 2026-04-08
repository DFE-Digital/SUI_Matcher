using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Application;

public sealed class PersonRecordOrchestrator(
    ILogger<PersonRecordOrchestrator> logger,
    IMatchingApiClient matchingApiClient,
    IOptions<StorageProcessFunctionOptions> options
) : IPersonRecordOrchestrator
{
    public async Task ProcessAsync(
        List<PersonSpecification> content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var stats = new MatchingProcessStats();

        foreach (var person in content)
        {
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
                    SearchStrategy = options.Value.SearchStrategy,
                    StrategyVersion = options.Value.StrategyVersion,
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
                logger.LogWarning(
                    ex,
                    "Failed to process row {RowNumber} in file {FileName}.",
                    fileName
                );
            }
        }
    }
}
