using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Functions;

namespace Unit.Tests.StorageProcessFunction;

public class ProcessStorageQueueMessageFunctionTests
{
    [Fact]
    public async Task Should_CallProcessAsync_When_RunAsyncIsInvoked()
    {
        var processor = new Mock<IStorageQueueMessageProcessor>();
        var queueMessage = new StorageBlobMessage
        {
            ContainerName = "incoming",
            BlobName = "test-file.csv",
        };
        var sut = new ProcessStorageQueueMessageFunction(
            processor.Object,
            NullLogger<ProcessStorageQueueMessageFunction>.Instance
        );

        await sut.RunAsync(queueMessage, CancellationToken.None);

        processor.Verify(x => x.ProcessAsync(queueMessage, CancellationToken.None), Times.Once);
    }
}
