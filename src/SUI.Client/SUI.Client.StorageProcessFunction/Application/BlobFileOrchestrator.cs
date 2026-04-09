using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Application;

public sealed class BlobFileOrchestrator(
    ILogger<BlobFileOrchestrator> logger,
    TimeProvider timeProvider,
    IBlobStorageClient blobStorageClient,
    IMatchPersonRecordOrchestrator<CsvRecordDto> matchPersonRecordOrchestrator,
    IOptions<StorageProcessFunctionOptions> options
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
        CsvHeaderValidator.Validate(listOfRecords.Headers, options.Value.CsvParserName);
        var csvRecords = listOfRecords.Records.Select(record => new CsvRecordDto(record)).ToList();

        var matchedResults = await matchPersonRecordOrchestrator.ProcessAsync(
            csvRecords,
            queueMessage.BlobName!,
            cancellationToken
        );

        // Just logging for now. Next: Process results
        logger.LogInformation(
            "Completed processing blob {BlobName}. Total records: {TotalRecords}, Successful matches: {SuccessfulMatches}.",
            queueMessage.BlobName,
            matchedResults.Count,
            matchedResults.Count(r => r.IsSuccess)
        );

        var processedBlobName = BuildProcessedBlobName(queueMessage.BlobName!);
        await blobStorageClient.ArchiveProcessedAsync(
            queueMessage,
            options.Value.ProcessedContainerName,
            processedBlobName,
            cancellationToken
        );
    }

    private string BuildProcessedBlobName(string blobName)
    {
        var fileName = Path.GetFileName(blobName);
        var fileNameNoExt = Path.GetFileNameWithoutExtension(blobName);

        var timestamp = timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"{timestamp}_{fileNameNoExt}/{fileName}";
    }
}
