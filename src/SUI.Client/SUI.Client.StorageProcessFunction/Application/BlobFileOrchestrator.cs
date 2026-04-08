using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Application;

public sealed class BlobFileOrchestrator(
    ILogger<BlobFileOrchestrator> logger,
    TimeProvider timeProvider,
    IBlobStorageClient blobStorageClient,
    IPersonRecordCsvParserFactory personSpecificationCsvParserFactory,
    IPersonRecordOrchestrator personRecordOrchestrator,
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
        var personSpecificationCsvParser = personSpecificationCsvParserFactory.Create(
            options.Value.CsvParserName
        );
        var personRecords = personSpecificationCsvParser.ParseListAsync(
            blobContent,
            queueMessage.BlobName!,
            cancellationToken
        );

        await personRecordOrchestrator.ProcessAsync(
            personRecords,
            queueMessage.BlobName!,
            cancellationToken
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

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException(
                "Queue message blobName did not contain a file name."
            );
        }

        var timestamp = timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"{timestamp}_{fileNameNoExt}/{fileName}";
    }
}
