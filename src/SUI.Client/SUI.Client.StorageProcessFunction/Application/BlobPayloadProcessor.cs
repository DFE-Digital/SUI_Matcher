using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.Http;

namespace SUI.StorageProcessFunction.Application;

public sealed class BlobPayloadProcessor(
    ILogger<BlobPayloadProcessor> logger,
    IBlobPersonSpecificationCsvParser blobPersonSpecificationCsvParser,
    IMatchingApiClient matchingApiClient,
    IOptions<StorageProcessFunctionOptions> options
) : IBlobPayloadProcessor
{
    public async Task ProcessAsync(BlobFileContent blobFile, CancellationToken cancellationToken)
    {
        var people = await blobPersonSpecificationCsvParser.ParseAsync(blobFile, cancellationToken);
        var stats = new MatchingProcessStats();

        logger.LogInformation(
            "Parsed {RecordCount} person records from blob {BlobName} in container {ContainerName}. Beginning Matching API sends.",
            people.Count,
            blobFile.Blob.BlobName,
            blobFile.Blob.ContainerName
        );

        for (var index = 0; index < people.Count; index++)
        {
            var person = people[index];
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
                logger.LogError(
                    ex,
                    "Failed to process row {RowNumber} from blob {BlobName}. Postcode={Postcode}",
                    index + 1,
                    blobFile.Blob.BlobName,
                    person.AddressPostalCode
                );
            }
        }

        logger.LogInformation(
            "Completed processing blob {BlobName}. Total={Total} Matched={Matched} PotentialMatch={PotentialMatch} LowConfidenceMatch={LowConfidenceMatch} ManyMatch={ManyMatch} NoMatch={NoMatch} Errors={Errors}.",
            blobFile.Blob.BlobName,
            stats.Count,
            stats.CountMatched,
            stats.CountPotentialMatch,
            stats.CountLowConfidenceMatch,
            stats.CountManyMatch,
            stats.CountNoMatch,
            stats.ErroredCount
        );
    }
}
