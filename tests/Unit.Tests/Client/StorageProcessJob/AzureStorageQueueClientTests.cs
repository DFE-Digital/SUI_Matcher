using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Moq;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Infrastructure.Azure;

namespace Unit.Tests.Client.StorageProcessJob;

public class AzureStorageQueueClientTests
{
    [Fact]
    public async Task Should_ReturnMessage_When_QueueContainsMessage()
    {
        var queueMessage = QueuesModelFactory.QueueMessage(
            messageId: "message-id",
            popReceipt: "pop-receipt",
            body: BinaryData.FromString("event-grid-message"),
            dequeueCount: 1,
            insertedOn: null,
            expiresOn: null,
            nextVisibleOn: null
        );
        var queueClient = new TestQueueClient(queueMessage);
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient);

        var result = await sut.FetchMessageAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("event-grid-message", result.MessageText);
        Assert.Equal("message-id", result.MessageId);
        Assert.Equal("pop-receipt", result.PopReceipt);
    }

    [Fact]
    public async Task Should_ReturnNull_When_QueueIsEmpty()
    {
        var queueClient = new TestQueueClient(null);
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient);

        var result = await sut.FetchMessageAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_FetchingMessage()
    {
        var queueClient = new TestQueueClient(null);
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient, queueName: "configured-queue");

        await sut.FetchMessageAsync(CancellationToken.None);

        Assert.Equal("configured-queue", queueServiceClient.QueueName);
    }

    [Fact]
    public async Task Should_DeleteMessage_When_MessageHasBeenProcessed()
    {
        var queueClient = new TestQueueClient(null);
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient);
        var message = new StorageQueueMessage("event-grid-message", "message-id", "pop-receipt");

        await sut.DeleteMessageAsync(message, CancellationToken.None);

        Assert.Equal("message-id", queueClient.DeletedMessageId);
        Assert.Equal("pop-receipt", queueClient.DeletedPopReceipt);
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_DeletingMessage()
    {
        var queueClient = new TestQueueClient(null);
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient, queueName: "configured-queue");
        var message = new StorageQueueMessage("event-grid-message", "message-id", "pop-receipt");

        await sut.DeleteMessageAsync(message, CancellationToken.None);

        Assert.Equal("configured-queue", queueServiceClient.QueueName);
    }

    [Fact]
    public async Task Should_ReturnMessageWithUpdatedPopReceipt_When_RenewingMessageVisibility()
    {
        var queueClient = new TestQueueClient(null, updatedPopReceipt: "updated-pop-receipt");
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient);
        var message = new StorageQueueMessage("event-grid-message", "message-id", "pop-receipt");

        var result = await sut.RenewMessageVisibilityAsync(
            message,
            TimeSpan.FromMinutes(10),
            CancellationToken.None
        );

        Assert.Equal("event-grid-message", result.MessageText);
        Assert.Equal("message-id", result.MessageId);
        Assert.Equal("updated-pop-receipt", result.PopReceipt);
    }

    [Fact]
    public async Task Should_UseCurrentReceiptAndVisibilityTimeout_When_RenewingMessageVisibility()
    {
        var queueClient = new TestQueueClient(null, updatedPopReceipt: "updated-pop-receipt");
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient);
        var message = new StorageQueueMessage("event-grid-message", "message-id", "pop-receipt");

        await sut.RenewMessageVisibilityAsync(
            message,
            TimeSpan.FromMinutes(5),
            CancellationToken.None
        );

        Assert.Equal("message-id", queueClient.UpdatedMessageId);
        Assert.Equal("pop-receipt", queueClient.UpdatedPopReceipt);
        Assert.Equal(TimeSpan.FromMinutes(5), queueClient.UpdatedVisibilityTimeout);
    }

    [Fact]
    public async Task Should_UseConfiguredQueueName_When_RenewingMessageVisibility()
    {
        var queueClient = new TestQueueClient(null, updatedPopReceipt: "updated-pop-receipt");
        var queueServiceClient = new TestQueueServiceClient(queueClient);
        var sut = BuildSut(queueServiceClient, queueName: "configured-queue");
        var message = new StorageQueueMessage("event-grid-message", "message-id", "pop-receipt");

        await sut.RenewMessageVisibilityAsync(
            message,
            TimeSpan.FromMinutes(10),
            CancellationToken.None
        );

        Assert.Equal("configured-queue", queueServiceClient.QueueName);
    }

    private static AzureStorageQueueClient BuildSut(
        QueueServiceClient queueServiceClient,
        string queueName = "storage-process-job"
    )
    {
        var options = Options.Create(
            new StorageProcessJobOptions { QueueName = queueName, CsvParserName = "TypeOne" }
        );

        return new AzureStorageQueueClient(queueServiceClient, options);
    }

    private sealed class TestQueueServiceClient(QueueClient queueClient) : QueueServiceClient
    {
        public string? QueueName { get; private set; }

        public override QueueClient GetQueueClient(string queueName)
        {
            QueueName = queueName;
            return queueClient;
        }
    }

    private sealed class TestQueueClient(
        QueueMessage? message,
        string updatedPopReceipt = "updated-pop-receipt"
    ) : QueueClient
    {
        public string? DeletedMessageId { get; private set; }
        public string? DeletedPopReceipt { get; private set; }
        public string? UpdatedMessageId { get; private set; }
        public string? UpdatedPopReceipt { get; private set; }
        public TimeSpan? UpdatedVisibilityTimeout { get; private set; }

        public override Task<Response<QueueMessage>> ReceiveMessageAsync(
            TimeSpan? visibilityTimeout = null,
            CancellationToken cancellationToken = default
        )
        {
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
