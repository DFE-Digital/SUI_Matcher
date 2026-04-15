using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Infrastructure.Azure;

[ExcludeFromCodeCoverage(
    Justification = "Unit testing would be all mocks. We could cover this with integration later"
)]
public sealed class AzureStorageQueueClient(
    QueueServiceClient queueServiceClient,
    IOptions<StorageProcessJobOptions> options
) : IStorageQueueClient
{
    public async Task<StorageQueueMessage?> FetchMessageAsync(CancellationToken cancellationToken)
    {
        var queueClient = queueServiceClient.GetQueueClient(options.Value.QueueName);
        var response = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
        var message = response?.Value;

        return message is null
            ? null
            : new StorageQueueMessage(message.MessageText, message.MessageId, message.PopReceipt);
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
}
