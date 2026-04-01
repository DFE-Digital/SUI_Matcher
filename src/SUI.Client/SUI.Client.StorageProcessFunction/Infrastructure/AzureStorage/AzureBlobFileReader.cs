using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Infrastructure.Interfaces;

namespace SUI.StorageProcessFunction.Infrastructure.AzureStorage;

[ExcludeFromCodeCoverage(
    Justification = "Unit testing would be all mocks. We could cover this with integration later"
)]
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

    public async Task ArchiveProcessedAsync(
        BlobFileContent blobFile,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken
    )
    {
        var sourceContainerClient = blobServiceClient.GetBlobContainerClient(
            blobFile.Blob.ContainerName!
        );
        var destinationContainerClient = blobServiceClient.GetBlobContainerClient(
            destinationContainerName
        );
        await destinationContainerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken
        );

        var sourceBlobClient = sourceContainerClient.GetBlobClient(blobFile.Blob.BlobName!);
        var destinationBlobClient = destinationContainerClient.GetBlobClient(destinationBlobName);

        await destinationBlobClient.UploadAsync(
            blobFile.Content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = blobFile.ContentType },
            },
            cancellationToken
        );

        await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
