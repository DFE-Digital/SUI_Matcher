using Microsoft.Extensions.Logging.Abstractions;
using SUI.StorageProcessFunction.Application;
using Unit.Tests.StorageProcessFunction.Stubs;

namespace Unit.Tests.StorageProcessFunction;

public class StorageQueueMessageProcessorTests
{
    [Fact]
    public async Task Should_ProcessBlob_When_QueueMessageIsValid()
    {
        var blobFileReader = new StubBlobFileReader();
        var blobPayloadProcessor = new StubBlobPayloadProcessor();
        var sut = new StorageQueueMessageProcessor(
            NullLogger<StorageQueueMessageProcessor>.Instance,
            blobFileReader,
            blobPayloadProcessor
        );

        await sut.ProcessAsync(
            new StorageBlobMessage { ContainerName = "incoming", BlobName = "test-file.csv" },
            CancellationToken.None
        );

        Assert.True(blobFileReader.ReadCalled);
        Assert.True(blobPayloadProcessor.ProcessCalled);
    }

    [Fact]
    public async Task Should_Throw_When_QueueMessageDoesNotContainContainerName()
    {
        var blobFileReader = new StubBlobFileReader();
        var blobPayloadProcessor = new StubBlobPayloadProcessor();
        var sut = new StorageQueueMessageProcessor(
            NullLogger<StorageQueueMessageProcessor>.Instance,
            blobFileReader,
            blobPayloadProcessor
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcessAsync(
                new StorageBlobMessage { BlobName = "test-file.csv" },
                CancellationToken.None
            )
        );

        Assert.False(blobFileReader.ReadCalled);
        Assert.False(blobPayloadProcessor.ProcessCalled);
    }

    [Fact]
    public async Task Should_Throw_When_QueueMessageDoesNotContainBlobName()
    {
        var blobFileReader = new StubBlobFileReader();
        var blobPayloadProcessor = new StubBlobPayloadProcessor();
        var sut = new StorageQueueMessageProcessor(
            NullLogger<StorageQueueMessageProcessor>.Instance,
            blobFileReader,
            blobPayloadProcessor
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcessAsync(
                new StorageBlobMessage { ContainerName = "incoming" },
                CancellationToken.None
            )
        );

        Assert.False(blobFileReader.ReadCalled);
        Assert.False(blobPayloadProcessor.ProcessCalled);
    }
}
