using System.Globalization;
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
    TimeProvider timeProvider,
    IBlobStorageClient blobStorageClient,
    IMatchPersonRecordOrchestrator<CsvRecordDto> matchPersonRecordOrchestrator,
    IMatchResultsService matchResultsService,
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

        var processedDirectory = BuildProcessedDirectoryName(queueMessage.BlobName!);
        var processedBlobName = BuildProcessedBlobName(processedDirectory, queueMessage.BlobName!);
        var fullResultsBlobName = BuildFullResultsBlobName(
            processedDirectory,
            queueMessage.BlobName!
        );

        await matchResultsService.ExportFullResultsAsync(
            fullResultsBlobName,
            queueMessage.BlobName!,
            matchedResults,
            cancellationToken
        );

        await blobStorageClient.ArchiveProcessedAsync(
            queueMessage,
            options.Value.ProcessedContainerName,
            processedBlobName,
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

    private string BuildProcessedDirectoryName(string blobName)
    {
        var fileNameNoExt = Path.GetFileNameWithoutExtension(blobName);
        var timestamp = timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return $"{timestamp}_{fileNameNoExt}";
    }

    private static string BuildProcessedBlobName(string processedDirectory, string blobName)
    {
        var fileName = Path.GetFileName(blobName);
        return $"{processedDirectory}/{fileName}";
    }

    private static string BuildFullResultsBlobName(string processedDirectory, string blobName)
    {
        var fileNameNoExt = Path.GetFileNameWithoutExtension(blobName);
        return $"{processedDirectory}/{fileNameNoExt}_full-results.csv";
    }
}
