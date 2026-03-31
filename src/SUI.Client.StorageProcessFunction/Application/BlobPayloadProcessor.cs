using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace SUI.StorageProcessFunction.Application;

[ExcludeFromCodeCoverage(Justification = "Function not implemented yet")]
public sealed class BlobPayloadProcessor(ILogger<BlobPayloadProcessor> logger)
    : IBlobPayloadProcessor
{
    public Task ProcessAsync(BlobFileContent blobFile, CancellationToken cancellationToken)
    {
        // placeholder logger to show it's reaching this stage
        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName} with {ByteCount} bytes.",
            blobFile.Blob.BlobName,
            blobFile.Blob.ContainerName,
            blobFile.Content.ToMemory().Length
        );

        // Next: reading and processing file contents.
        return Task.CompletedTask;
    }
}
