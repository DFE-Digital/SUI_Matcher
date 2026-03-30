using Azure.Storage.Blobs;
using SUI.StorageProcessFunction.Application;

namespace SUI.StorageProcessFunction.Infrastructure.AzureStorage;

public sealed class AzureBlobFileReader(BlobServiceClient blobServiceClient) : IBlobFileReader
{
    public async Task<BlobFileContent> ReadAsync(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    )
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(blobMessage.ContainerName!)
            .GetBlobClient(blobMessage.BlobName!);

        var download = await blobClient.DownloadContentAsync(cancellationToken);

        return new BlobFileContent(
            blobMessage,
            download.Value.Content,
            download.Value.Details.ContentType
        );
    }
}
