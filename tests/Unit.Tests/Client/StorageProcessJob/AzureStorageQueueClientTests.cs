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

        Assert.Equal(ConfiguredQueueName, harness.QueueService.MainQueueName);
    }

    [Fact]
    public async Task Should_UseConfiguredVisibilityTimeout_When_FetchingMessage()
    {
        var harness = new TestHarness(visibilityTimeoutMinutes: 10);

        await harness.Sut.FetchMessageAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromMinutes(10), harness.MainQueue.ReceivedVisibilityTimeout);
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
        Assert.Equal(MessageId, result.MessageId);
        Assert.Null(harness.QueueService.PoisonQueueName);
        Assert.Null(harness.MainQueue.DeletedMessageId);
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
        Assert.Equal($"{DefaultQueueName}-poison", harness.QueueService.PoisonQueueName);
        Assert.Equal(MessageText, harness.PoisonQueue.SentMessageText);
        Assert.Equal(MessageId, harness.MainQueue.DeletedMessageId);
        Assert.Equal(PopReceipt, harness.MainQueue.DeletedPopReceipt);
    }

    [Fact]
    public async Task Should_DeleteMessage_When_MessageHasBeenProcessed()
    {
        var harness = new TestHarness();
        var message = CreateStorageQueueMessage();

        await harness.Sut.DeleteMessageAsync(message, CancellationToken.None);

        Assert.Equal(MessageId, harness.MainQueue.DeletedMessageId);
        Assert.Equal(PopReceipt, harness.MainQueue.DeletedPopReceipt);
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_DeletingMessage()
    {
        var harness = new TestHarness(queueName: ConfiguredQueueName);
        var message = CreateStorageQueueMessage();

        await harness.Sut.DeleteMessageAsync(message, CancellationToken.None);

        Assert.Equal(ConfiguredQueueName, harness.QueueService.MainQueueName);
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

        Assert.Equal(MessageText, result.MessageText);
        Assert.Equal(MessageId, result.MessageId);
        Assert.Equal(UpdatedPopReceipt, result.PopReceipt);
    }

    [Fact]
    public async Task Should_UseCurrentReceiptAndVisibilityTimeout_When_RenewingMessageVisibility()
    {
        var harness = new TestHarness();
        var message = CreateStorageQueueMessage();

        await harness.Sut.RenewMessageVisibilityAsync(
            message,
            TimeSpan.FromMinutes(5),
            CancellationToken.None
        );

        Assert.Equal(MessageId, harness.MainQueue.UpdatedMessageId);
        Assert.Equal(PopReceipt, harness.MainQueue.UpdatedPopReceipt);
        Assert.Equal(TimeSpan.FromMinutes(5), harness.MainQueue.UpdatedVisibilityTimeout);
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_RenewingMessageVisibility()
    {
        var harness = new TestHarness(queueName: ConfiguredQueueName);
        var message = CreateStorageQueueMessage();

        await harness.Sut.RenewMessageVisibilityAsync(
            message,
            TimeSpan.FromMinutes(10),
            CancellationToken.None
        );

        Assert.Equal(ConfiguredQueueName, harness.QueueService.MainQueueName);
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
        public QueueClientSpy MainQueue { get; }
        public QueueClientSpy PoisonQueue { get; }
        public QueueServiceClientSpy QueueService { get; }
        public AzureStorageQueueClient Sut { get; }

        public TestHarness(
            QueueMessage? receivedMessage = null,
            string queueName = DefaultQueueName,
            int visibilityTimeoutMinutes = 10,
            int maxDequeueCount = 1
        )
        {
            MainQueue = new QueueClientSpy(receivedMessage);
            PoisonQueue = new QueueClientSpy(null);
            QueueService = new QueueServiceClientSpy(MainQueue, PoisonQueue);

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
                QueueService,
                options
            );
        }
    }

    private sealed class QueueServiceClientSpy(QueueClientSpy mainQueue, QueueClientSpy poisonQueue)
        : QueueServiceClient
    {
        public string? MainQueueName { get; private set; }
        public string? PoisonQueueName { get; private set; }

        public override QueueClient GetQueueClient(string queueName)
        {
            if (queueName.EndsWith("-poison", StringComparison.Ordinal))
            {
                PoisonQueueName = queueName;
                return poisonQueue;
            }

            MainQueueName = queueName;
            return mainQueue;
        }
    }

    private sealed class QueueClientSpy(
        QueueMessage? message,
        string updatedPopReceipt = UpdatedPopReceipt
    ) : QueueClient
    {
        public string? DeletedMessageId { get; private set; }
        public string? DeletedPopReceipt { get; private set; }
        public string? UpdatedMessageId { get; private set; }
        public string? UpdatedPopReceipt { get; private set; }
        public TimeSpan? UpdatedVisibilityTimeout { get; private set; }
        public TimeSpan? ReceivedVisibilityTimeout { get; private set; }
        public string? SentMessageText { get; private set; }

        public override Task<Response<QueueMessage>> ReceiveMessageAsync(
            TimeSpan? visibilityTimeout = null,
            CancellationToken cancellationToken = default
        )
        {
            ReceivedVisibilityTimeout = visibilityTimeout;

            return Task.FromResult(Response.FromValue(message!, Mock.Of<Response>()));
        }

        public override Task<Response> DeleteMessageAsync(
            string messageId,
            string popReceipt,
            CancellationToken cancellationToken = default
        )
        {
            DeletedMessageId = messageId;
            DeletedPopReceipt = popReceipt;

            return Task.FromResult(Mock.Of<Response>());
        }

        public override Task<Response<SendReceipt>> SendMessageAsync(
            string messageText,
            TimeSpan? visibilityTimeout = null,
            TimeSpan? timeToLive = null,
            CancellationToken cancellationToken = default
        )
        {
            SentMessageText = messageText;

            var sendReceipt = QueuesModelFactory.SendReceipt(
                "poison-message-id",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(7),
                "poison-pop-receipt",
                DateTimeOffset.UtcNow
            );

            return Task.FromResult(Response.FromValue(sendReceipt, Mock.Of<Response>()));
        }

        public override Task<Response<UpdateReceipt>> UpdateMessageAsync(
            string messageId,
            string popReceipt,
            string messageText = null!,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default
        )
        {
            UpdatedMessageId = messageId;
            UpdatedPopReceipt = popReceipt;
            UpdatedVisibilityTimeout = visibilityTimeout;

            var updateReceipt = QueuesModelFactory.UpdateReceipt(
                popReceipt: updatedPopReceipt,
                nextVisibleOn: DateTimeOffset.UtcNow.Add(visibilityTimeout)
            );

            return Task.FromResult(Response.FromValue(updateReceipt, Mock.Of<Response>()));
        }
    }
}
