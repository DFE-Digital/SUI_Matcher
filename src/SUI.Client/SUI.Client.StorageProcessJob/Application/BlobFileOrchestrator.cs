using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Application;

public sealed class BlobFileOrchestrator(
    ILogger<BlobFileOrchestrator> logger,
    IBlobStorageClient blobStorageClient,
    IMatchPersonRecordOrchestrator<CsvRecordDto> matchPersonRecordOrchestrator,
    IMatchResultsService matchResultsService,
    MatchResultsBlobNameBuilder matchResultsBlobNameBuilder,
    ICsvHeadersProvider csvHeadersProvider,
    IOptions<StorageProcessJobOptions> options
) : IBlobFileOrchestrator
{
    public async Task ProcessAsync(
        StorageBlobMessage queueMessage,
        CancellationToken cancellationToken
    )
    {
        var messageValidation = queueMessage.Validate();

        if (!messageValidation.IsValid)
        {
            throw new InvalidOperationException(messageValidation.ValidationMessage);
        }

        logger.LogInformation(
            "Processing blob {BlobName} from container {ContainerName}.",
            queueMessage.BlobName,
            queueMessage.ContainerName
        );

        var blobContent = await blobStorageClient.GetBlobContents(queueMessage, cancellationToken);

        var blobStream = new StreamReader(blobContent.ToStream());
        var listOfRecords = await CsvRecordReader.ReadCsvTextAsync(blobStream, cancellationToken);
        CsvHeaderValidator.Validate(listOfRecords.Headers, csvHeadersProvider.GetRequiredHeaders());
        CheckOptionalCsvHeaders(queueMessage, listOfRecords);

        var csvRecords = listOfRecords.Records.Select(record => new CsvRecordDto(record)).ToList();

        var matchedResults = await matchPersonRecordOrchestrator.ProcessAsync(
            csvRecords,
            queueMessage.BlobName!,
            cancellationToken
        );

        logger.LogInformation(
            "Completed processing blob {BlobName}. Total records: {TotalRecords}, Successful requests: {SuccessfulRequests}.",
            queueMessage.BlobName,
            matchedResults.Count,
            matchedResults.Count(r => r.IsSuccess)
        );

        await matchResultsService.ExportSuccessResultsAsync(
            queueMessage.BlobName!,
            matchedResults,
            cancellationToken
        );

        await matchResultsService.ExportFullResultsAsync(
            queueMessage.BlobName!,
            matchedResults,
            cancellationToken
        );

        var archivedOriginalBlobName = matchResultsBlobNameBuilder.BuildArchivedOriginalBlobName(
            queueMessage.BlobName!
        );

        await blobStorageClient.ArchiveProcessedAsync(
            queueMessage,
            options.Value.ProcessedContainerName,
            archivedOriginalBlobName,
            cancellationToken
        );
    }

    /// <summary>
    /// Logs if any optional headers are missing.
    /// </summary>
    /// <param name="queueMessage"></param>
    /// <param name="listOfRecords"></param>
    private void CheckOptionalCsvHeaders(
        StorageBlobMessage queueMessage,
        (HashSet<string> Headers, List<Dictionary<string, string>> Records) listOfRecords
    )
    {
        var missingOptionalHeaders = CsvHeaderValidator.GetMissingHeaders(
            listOfRecords.Headers,
            csvHeadersProvider.GetOptionalHeaders()
        );

        if (missingOptionalHeaders.Length > 0)
        {
            logger.LogWarning(
                "Missing optional CSV headers: {MissingOptionalHeaders}. On {BlobName}",
                string.Join(", ", missingOptionalHeaders),
                queueMessage.BlobName
            );
        }
    }
}
