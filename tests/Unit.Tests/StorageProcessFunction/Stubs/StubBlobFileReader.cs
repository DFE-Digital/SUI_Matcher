using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction.Stubs;

public sealed class StubBlobFileReader : IBlobFileReader
{
    public bool ReadCalled { get; private set; }
    public bool ArchiveProcessedCalled { get; private set; }
    public string? ArchivedContainerName { get; private set; }
    public string? ArchivedBlobName { get; private set; }

    public Task<BlobFileContent> ReadAsync(
        StorageBlobMessage blobMessage,
        CancellationToken cancellationToken
    )
    {
        ReadCalled = true;
        return Task.FromResult(
            new BlobFileContent(blobMessage, BinaryData.FromString("test"), "text/plain")
        );
    }

    public Task ArchiveProcessedAsync(
        BlobFileContent blobFile,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken
    )
    {
        ArchiveProcessedCalled = true;
        ArchivedContainerName = destinationContainerName;
        ArchivedBlobName = destinationBlobName;
        return Task.CompletedTask;
    }
}
