using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Infrastructure.Azure;

public sealed class AzureStorageQueueClient(
    ILogger<AzureStorageQueueClient> logger,
    QueueServiceClient queueServiceClient,
    IOptions<StorageProcessJobOptions> options
) : IStorageQueueClient
{
    public async Task<StorageQueueMessage?> FetchMessageAsync(CancellationToken cancellationToken)
    {
        var queueClient = queueServiceClient.GetQueueClient(options.Value.QueueName);
        var response = await queueClient.ReceiveMessageAsync(
            visibilityTimeout: TimeSpan.FromMinutes(options.Value.MessageVisibilityTimeoutMinutes),
            cancellationToken: cancellationToken
        );
        var message = response?.Value;

        if (message is null)
        {
            return null;
        }

        if (message.DequeueCount > options.Value.MaxDequeueCount)
        {
            var poisonQueueName = $"{options.Value.QueueName}-poison";
            logger.LogWarning(
                "Moving queue message {MessageId} to poison queue {PoisonQueueName} after {DequeueCount} delivery attempts.",
                message.MessageId,
                poisonQueueName,
                message.DequeueCount
            );

            var poisonQueueClient = queueServiceClient.GetQueueClient(poisonQueueName);
            await poisonQueueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            await poisonQueueClient.SendMessageAsync(
                message.MessageText,
                cancellationToken: cancellationToken
            );

            await queueClient.DeleteMessageAsync(
                message.MessageId,
                message.PopReceipt,
                cancellationToken
            );

            return null;
        }

        return new StorageQueueMessage(message.MessageText, message.MessageId, message.PopReceipt);
    }

    public async Task DeleteMessageAsync(
        StorageQueueMessage message,
        CancellationToken cancellationToken
    )
    {
        var queueClient = queueServiceClient.GetQueueClient(options.Value.QueueName);

        await queueClient.DeleteMessageAsync(
            message.MessageId,
            message.PopReceipt,
            cancellationToken
        );
    }

    public async Task<StorageQueueMessage> RenewMessageVisibilityAsync(
        StorageQueueMessage message,
        TimeSpan visibilityTimeout,
        CancellationToken cancellationToken
    )
    {
        var queueClient = queueServiceClient.GetQueueClient(options.Value.QueueName);

        var response = await queueClient.UpdateMessageAsync(
            message.MessageId,
            message.PopReceipt,
            visibilityTimeout: visibilityTimeout,
            cancellationToken: cancellationToken
        );

        var updatedMessage = response?.Value;

        if (updatedMessage is null)
        {
            throw new InvalidOperationException("Failed to renew message visibility.");
        }

        return message with
        {
            PopReceipt = updatedMessage.PopReceipt,
        };
    }
}
