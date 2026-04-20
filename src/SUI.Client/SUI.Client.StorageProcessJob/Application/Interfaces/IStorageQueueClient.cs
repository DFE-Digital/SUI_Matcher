namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IStorageQueueClient
{
    Task<StorageQueueMessage?> FetchMessageAsync(CancellationToken cancellationToken);

    Task DeleteMessageAsync(StorageQueueMessage message, CancellationToken cancellationToken);
    Task<StorageQueueMessage> RenewMessageVisibilityAsync(
        StorageQueueMessage message,
        TimeSpan visibilityTimeout,
        CancellationToken cancellationToken
    );
}
