using System.Globalization;
using Microsoft.Extensions.Logging;

namespace SUI.StorageProcessFunction.Application;

public sealed class StorageQueueMessageProcessor(
    ILogger<StorageQueueMessageProcessor> logger,
    TimeProvider timeProvider,
    IBlobFileReader blobFileReader,
    IBlobPayloadProcessor blobPayloadProcessor
) : IStorageQueueMessageProcessor
{
    private const string ProcessedContainerName = "processed";

    public async Task ProcessAsync(
        StorageBlobMessage queueMessage,
        CancellationToken cancellationToken
    )
    {
        Validate(queueMessage);

        logger.LogInformation(
            "Processing blob {BlobName} from container {ContainerName}.",
            queueMessage.BlobName,
            queueMessage.ContainerName
        );

        var blobFile = await blobFileReader.ReadAsync(queueMessage, cancellationToken);
        await blobPayloadProcessor.ProcessAsync(blobFile, cancellationToken);
        var processedBlobName = BuildProcessedBlobName(queueMessage.BlobName!);
        await blobFileReader.ArchiveProcessedAsync(
            blobFile,
            ProcessedContainerName,
            processedBlobName,
            cancellationToken
        );
    }

    private static void Validate(StorageBlobMessage queueMessage)
    {
        if (string.IsNullOrWhiteSpace(queueMessage.ContainerName))
        {
            throw new InvalidOperationException("Queue message did not contain containerName.");
        }

        if (string.IsNullOrWhiteSpace(queueMessage.BlobName))
        {
            throw new InvalidOperationException("Queue message did not contain blobName.");
        }
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
