namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IBlobStorageClient
{
    Task<Stream> OpenReadAsync(StorageBlobMessage blobMessage, CancellationToken cancellationToken);

    Task ArchiveProcessedAsync(
        StorageBlobMessage blobMessage,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken
    );
}
