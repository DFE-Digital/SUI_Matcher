using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction;

public class StorageQueueMessageProcessorTests
{
    private readonly Mock<IBlobFileReader> _blobFileReader;
    private readonly Mock<IBlobPayloadProcessor> _blobPayloadProcessor;
    private readonly StorageQueueMessageProcessor _sut;
    private readonly FakeTimeProvider _timeProvider;

    public StorageQueueMessageProcessorTests()
    {
        _blobFileReader = new Mock<IBlobFileReader>();
        _blobPayloadProcessor = new Mock<IBlobPayloadProcessor>();
        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
        );
        _sut = new StorageQueueMessageProcessor(
            NullLogger<StorageQueueMessageProcessor>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            _blobPayloadProcessor.Object
        );
    }

    [Fact]
    public async Task Should_ProcessBlob_When_QueueMessageIsValid()
    {
        var queueMessage = new StorageBlobMessage
        {
            ContainerName = "incoming",
            BlobName = "test-file.csv",
        };
        var blobFile = new BlobFileContent(
            queueMessage,
            BinaryData.FromString("test"),
            "text/plain"
        );

        _blobFileReader
            .Setup(x => x.ReadAsync(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobFile);

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _blobFileReader.Verify(x => x.ReadAsync(queueMessage, CancellationToken.None), Times.Once);
        _blobPayloadProcessor.Verify(
            x => x.ProcessAsync(blobFile, CancellationToken.None),
            Times.Once
        );
        var utcNow = $"{_timeProvider.GetUtcNow():yyyyMMddHHmmss}_test-file/test-file.csv";
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    blobFile,
                    "processed",
                    It.Is<string>(name => name == utcNow),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Throw_When_QueueMessageDoesNotContainContainerName()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(
                new StorageBlobMessage { BlobName = "test-file.csv" },
                CancellationToken.None
            )
        );

        _blobFileReader.Verify(
            x => x.ReadAsync(It.IsAny<StorageBlobMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _blobPayloadProcessor.Verify(
            x => x.ProcessAsync(It.IsAny<BlobFileContent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    It.IsAny<BlobFileContent>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Throw_When_QueueMessageDoesNotContainBlobName()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(
                new StorageBlobMessage { ContainerName = "incoming" },
                CancellationToken.None
            )
        );

        _blobFileReader.Verify(
            x => x.ReadAsync(It.IsAny<StorageBlobMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _blobPayloadProcessor.Verify(
            x => x.ProcessAsync(It.IsAny<BlobFileContent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    It.IsAny<BlobFileContent>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }
}
