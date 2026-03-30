using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction.Stubs;

public sealed class StubBlobFileReader : IBlobFileReader
{
    public bool ReadCalled { get; private set; }

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
}
