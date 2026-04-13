using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;
using SUI.StorageProcessFunction.Exceptions;
using SUI.StorageProcessFunction.Functions;

namespace Unit.Tests.StorageProcessFunction;

public class ProcessStorageQueueMessageFunctionTests
{
    [Fact]
    public async Task Should_CallProcessAsync_When_RunAsyncIsInvoked()
    {
        var queueMessageParser = new Mock<IStorageQueueMessageParser>();
        var processor = new Mock<IBlobFileOrchestrator>();
        var rawQueueMessage = BuildQueueMessage(
            "/blobServices/default/containers/incoming/blobs/test-file.csv"
        );
        var parsedQueueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        queueMessageParser.Setup(x => x.Parse(rawQueueMessage)).Returns(parsedQueueMessage);
        var sut = new ProcessStorageQueueMessageFunction(
            queueMessageParser.Object,
            processor.Object,
            NullLogger<ProcessStorageQueueMessageFunction>.Instance
        );

        await sut.RunAsync(rawQueueMessage, CancellationToken.None);

        queueMessageParser.Verify(x => x.Parse(rawQueueMessage), Times.Once);
        processor.Verify(
            x => x.ProcessAsync(parsedQueueMessage, CancellationToken.None),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_RethrowException_When_QueueMessageIsInvalid()
    {
        var queueMessageParser = new Mock<IStorageQueueMessageParser>();
        var processor = new Mock<IBlobFileOrchestrator>();
        var rawQueueMessage = BuildQueueMessage(
            "/blobServices/default/containers/incoming/blobs/test-file.csv"
        );
        var expectedException = new InvalidStorageQueueMessageException(
            "Queue message was invalid."
        );
        queueMessageParser.Setup(x => x.Parse(rawQueueMessage)).Throws(expectedException);
        var sut = new ProcessStorageQueueMessageFunction(
            queueMessageParser.Object,
            processor.Object,
            NullLogger<ProcessStorageQueueMessageFunction>.Instance
        );

        var actualException = await Assert.ThrowsAsync<InvalidStorageQueueMessageException>(() =>
            sut.RunAsync(rawQueueMessage, CancellationToken.None)
        );

        Assert.Same(expectedException, actualException);
        queueMessageParser.Verify(x => x.Parse(rawQueueMessage), Times.Once);
        processor.Verify(
            x => x.ProcessAsync(It.IsAny<StorageBlobMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private static string BuildQueueMessage(string subject) =>
        $$"""
            [
              {
                "id": "11111111-1111-1111-1111-111111111111",
                "subject": "{{subject}}",
                "eventType": "Microsoft.Storage.BlobCreated",
                "eventTime": "2026-04-01T12:00:00Z",
                "data": {},
                "dataVersion": "1",
                "metadataVersion": "1"
              }
            ]
            """;
}
