using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction.Stubs;

public sealed class StubBlobPayloadProcessor : IBlobPayloadProcessor
{
    public bool ProcessCalled { get; private set; }

    public Task ProcessAsync(BlobFileContent blobFile, CancellationToken cancellationToken)
    {
        ProcessCalled = true;
        return Task.CompletedTask;
    }
}
