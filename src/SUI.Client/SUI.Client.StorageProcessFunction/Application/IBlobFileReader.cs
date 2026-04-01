namespace SUI.StorageProcessFunction.Application;

public interface IBlobFileReader
{
    Task<BlobFileContent> ReadAsync(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    );

    Task ArchiveProcessedAsync(
        BlobFileContent blobFile,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken
    );
}
