using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Infrastructure.Azure;

[ExcludeFromCodeCoverage(
    Justification = "Unit testing would be all mocks. We could cover this with integration later"
)]
public sealed class AzureBlobStorageClient(BlobServiceClient blobServiceClient) : IBlobStorageClient
{
    public async Task<BinaryData> GetBlobContents(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    )
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(blobMessage.ContainerName!)
            .GetBlobClient(blobMessage.BlobName!);

        var response = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task UploadBlobAsync(
        string destinationContainerName,
        string destinationBlobName,
        BinaryData content,
        string contentType,
        CancellationToken cancellationToken
    )
    {
        var destinationContainerClient = blobServiceClient.GetBlobContainerClient(
            destinationContainerName
        );
        await destinationContainerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken
        );

        var destinationBlobClient = destinationContainerClient.GetBlobClient(destinationBlobName);
        await destinationBlobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            cancellationToken
        );
    }

    public async Task ArchiveProcessedAsync(
        StorageBlobMessage blobMessage,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken
    )
    {
        var sourceContainerClient = blobServiceClient.GetBlobContainerClient(
            blobMessage.ContainerName!
        );
        var destinationContainerClient = blobServiceClient.GetBlobContainerClient(
            destinationContainerName
        );
        await destinationContainerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken
        );

        var sourceBlobClient = sourceContainerClient.GetBlobClient(blobMessage.BlobName!);
        var destinationBlobClient = destinationContainerClient.GetBlobClient(destinationBlobName);
        var sourceBlobProperties = await sourceBlobClient.GetPropertiesAsync(
            cancellationToken: cancellationToken
        );
        await using var sourceStream = await sourceBlobClient.OpenReadAsync(
            cancellationToken: cancellationToken
        );

        await destinationBlobClient.UploadAsync(
            sourceStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = sourceBlobProperties.Value.ContentType,
                },
            },
            cancellationToken
        );

        await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
