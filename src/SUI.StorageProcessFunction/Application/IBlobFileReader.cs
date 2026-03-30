namespace SUI.StorageProcessFunction.Application;

public interface IBlobFileReader
{
    Task<BlobFileContent> ReadAsync(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    );
}
