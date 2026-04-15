using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Moq;
using SUI.Client.StorageProcessJob;
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

    private sealed class TestQueueClient(QueueMessage? message) : QueueClient
    {
        public override Task<Response<QueueMessage>> ReceiveMessageAsync(
            TimeSpan? visibilityTimeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Response.FromValue(message!, Mock.Of<Response>()));
        }
    }
}
