using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Application;

public sealed class PersonSpecificationFileOrchestrator(
    ILogger<PersonSpecificationFileOrchestrator> logger,
    IPersonSpecificationCsvParser personSpecificationCsvParser,
    IMatchingApiClient matchingApiClient,
    IOptions<StorageProcessFunctionOptions> options
) : IPersonSpecificationFileOrchestrator
{
    public async Task ProcessAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var stats = new MatchingProcessStats();
        var rowNumber = 0;

        await foreach (
            var person in personSpecificationCsvParser.ParseAsync(
                content,
                fileName,
                cancellationToken
            )
        )
        {
            rowNumber++;

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
                    rowNumber,
                    fileName
                );
            }
        }
    }
}
