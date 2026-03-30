using Microsoft.Extensions.Logging;

namespace SUI.StorageProcessFunction.Application;

public sealed class PlaceholderBlobPayloadProcessor(ILogger<PlaceholderBlobPayloadProcessor> logger)
    : IBlobPayloadProcessor
{
    public Task ProcessAsync(BlobFileContent blobFile, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName} with {ByteCount} bytes.",
            blobFile.Blob.BlobName,
            blobFile.Blob.ContainerName,
            blobFile.Content.ToMemory().Length
        );

        throw new NotSupportedException(
            "Blob processing has not been implemented yet. Add a format-specific processor here and reuse SUI.Client.Core where it fits."
        );
    }
}
