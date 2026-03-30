using Microsoft.Extensions.Logging;

namespace SUI.StorageProcessFunction.Application;

public sealed class StorageQueueMessageProcessor(
    ILogger<StorageQueueMessageProcessor> logger,
    IBlobFileReader blobFileReader,
    IBlobPayloadProcessor blobPayloadProcessor
)
{
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
}
