namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IBlobStorageClient
{
    Task<BinaryData> GetBlobContents(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    );

    Task ArchiveProcessedAsync(
        StorageBlobMessage blobMessage,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken
    );
}
