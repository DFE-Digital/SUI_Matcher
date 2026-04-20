using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Infrastructure.Azure;

namespace Unit.Tests.Client.StorageProcessJob;

public class AzureStorageQueueClientTests
{
    private const string DefaultQueueName = "storage-process-job";
    private const string ConfiguredQueueName = "configured-queue";
    private const string MessageId = "message-id";
    private const string PopReceipt = "pop-receipt";
    private const string MessageText = "event-grid-message";
    private const string UpdatedPopReceipt = "updated-pop-receipt";

    [Fact]
    public async Task Should_ReturnMessage_When_QueueContainsMessage()
    {
        var harness = new TestHarness(receivedMessage: CreateQueueMessage(dequeueCount: 1));

        var result = await harness.Sut.FetchMessageAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MessageText, result.MessageText);
        Assert.Equal(MessageId, result.MessageId);
        Assert.Equal(PopReceipt, result.PopReceipt);
    }

    [Fact]
    public async Task Should_ReturnNull_When_QueueIsEmpty()
    {
        var harness = new TestHarness();

        var result = await harness.Sut.FetchMessageAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_FetchingMessage()
    {
        var harness = new TestHarness(queueName: ConfiguredQueueName);

        await harness.Sut.FetchMessageAsync(CancellationToken.None);

        harness.QueueService.Verify(x => x.GetQueueClient(ConfiguredQueueName));
    }

    [Fact]
    public async Task Should_UseConfiguredVisibilityTimeout_When_FetchingMessage()
    {
        var visibilityTimeout = TimeSpan.FromMinutes(10);
        var harness = new TestHarness(
            visibilityTimeoutMinutes: (int)visibilityTimeout.TotalMinutes
        );

        await harness.Sut.FetchMessageAsync(CancellationToken.None);

        harness.MainQueue.Verify(x =>
            x.ReceiveMessageAsync(visibilityTimeout, It.IsAny<CancellationToken>())
        );
    }

    [Fact]
    public async Task Should_ReturnMessage_When_DequeueCountMatchesConfiguredMaximum()
    {
        var harness = new TestHarness(
            receivedMessage: CreateQueueMessage(dequeueCount: 1),
            maxDequeueCount: 1
        );

        var result = await harness.Sut.FetchMessageAsync(CancellationToken.None);

        Assert.NotNull(result);
        harness.QueueService.Verify(
            x => x.GetQueueClient(It.Is<string>(s => s.EndsWith("-poison"))),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_MoveMessageToPoisonQueue_When_DequeueCountExceedsConfiguredMaximum()
    {
        var harness = new TestHarness(
            receivedMessage: CreateQueueMessage(dequeueCount: 2),
            maxDequeueCount: 1
        );

        var result = await harness.Sut.FetchMessageAsync(CancellationToken.None);

        Assert.Null(result);
        harness.QueueService.Verify(x => x.GetQueueClient($"{DefaultQueueName}-poison"));
        harness.PoisonQueue.Verify(x =>
            x.SendMessageAsync(MessageText, It.IsAny<CancellationToken>())
        );
        harness.MainQueue.Verify(x =>
            x.DeleteMessageAsync(MessageId, PopReceipt, It.IsAny<CancellationToken>())
        );
    }

    [Fact]
    public async Task Should_DeleteMessage_When_MessageHasBeenProcessed()
    {
        var harness = new TestHarness();
        var message = CreateStorageQueueMessage();

        await harness.Sut.DeleteMessageAsync(message, CancellationToken.None);

        harness.MainQueue.Verify(x =>
            x.DeleteMessageAsync(MessageId, PopReceipt, It.IsAny<CancellationToken>())
        );
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_DeletingMessage()
    {
        var harness = new TestHarness(queueName: ConfiguredQueueName);
        var message = CreateStorageQueueMessage();

        await harness.Sut.DeleteMessageAsync(message, CancellationToken.None);

        harness.QueueService.Verify(x => x.GetQueueClient(ConfiguredQueueName));
    }

    [Fact]
    public async Task Should_ReturnMessageWithUpdatedPopReceipt_When_RenewingMessageVisibility()
    {
        var harness = new TestHarness();
        var message = CreateStorageQueueMessage();

        var result = await harness.Sut.RenewMessageVisibilityAsync(
            message,
            TimeSpan.FromMinutes(10),
            CancellationToken.None
        );

        Assert.Equal(UpdatedPopReceipt, result.PopReceipt);
    }

    [Fact]
    public async Task Should_UseCurrentReceiptAndVisibilityTimeout_When_RenewingMessageVisibility()
    {
        var harness = new TestHarness();
        var message = CreateStorageQueueMessage();
        var timeout = TimeSpan.FromMinutes(5);

        await harness.Sut.RenewMessageVisibilityAsync(message, timeout, CancellationToken.None);

        harness.MainQueue.Verify(x =>
            x.UpdateMessageAsync(
                MessageId,
                PopReceipt,
                (string?)null,
                timeout,
                It.IsAny<CancellationToken>()
            )
        );
    }

    private static StorageQueueMessage CreateStorageQueueMessage() =>
        new(MessageText, MessageId, PopReceipt);

    private static QueueMessage CreateQueueMessage(int dequeueCount) =>
        QueuesModelFactory.QueueMessage(
            messageId: MessageId,
            popReceipt: PopReceipt,
            body: BinaryData.FromString(MessageText),
            dequeueCount: dequeueCount,
            insertedOn: null,
            expiresOn: null,
            nextVisibleOn: null
        );

    private sealed class TestHarness
    {
        public Mock<QueueClient> MainQueue { get; } = new();
        public Mock<QueueClient> PoisonQueue { get; } = new();
        public Mock<QueueServiceClient> QueueService { get; } = new();
        public AzureStorageQueueClient Sut { get; }

        public TestHarness(
            QueueMessage? receivedMessage = null,
            string queueName = DefaultQueueName,
            int visibilityTimeoutMinutes = 10,
            int maxDequeueCount = 1
        )
        {
            QueueService
                .Setup(x => x.GetQueueClient(It.IsAny<string>()))
                .Returns(
                    (string name) =>
                        name.EndsWith("-poison") ? PoisonQueue.Object : MainQueue.Object
                );

            MainQueue
                .Setup(x =>
                    x.ReceiveMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(Response.FromValue(receivedMessage!, Mock.Of<Response>()));

            MainQueue
                .Setup(x =>
                    x.DeleteMessageAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Mock.Of<Response>());

            MainQueue
                .Setup(x =>
                    x.UpdateMessageAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    (string _, string _, string _, TimeSpan timeout, CancellationToken _) =>
                        Response.FromValue(
                            QueuesModelFactory.UpdateReceipt(
                                UpdatedPopReceipt,
                                DateTimeOffset.UtcNow.Add(timeout)
                            ),
                            Mock.Of<Response>()
                        )
                );

            PoisonQueue
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Response.FromValue(
                        QueuesModelFactory.SendReceipt(
                            "id",
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            "receipt",
                            DateTimeOffset.UtcNow
                        ),
                        Mock.Of<Response>()
                    )
                );

            var options = Options.Create(
                new StorageProcessJobOptions
                {
                    QueueName = queueName,
                    MaxDequeueCount = maxDequeueCount,
                    MessageVisibilityTimeoutMinutes = visibilityTimeoutMinutes,
                    CsvParserName = "TypeOne",
                }
            );

            Sut = new AzureStorageQueueClient(
                NullLogger<AzureStorageQueueClient>.Instance,
                QueueService.Object,
                options
            );
        }
    }
}
