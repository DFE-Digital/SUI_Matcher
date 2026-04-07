using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Infrastructure.Azure;

[ExcludeFromCodeCoverage(
    Justification = "Unit testing would be all mocks. We could cover this with integration later"
)]
public sealed class AzureBlobStorageClient(BlobServiceClient blobServiceClient) : IBlobStorageClient
{
    public async Task<Stream> OpenReadAsync(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    )
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(blobMessage.ContainerName!)
            .GetBlobClient(blobMessage.BlobName!);

        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
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
