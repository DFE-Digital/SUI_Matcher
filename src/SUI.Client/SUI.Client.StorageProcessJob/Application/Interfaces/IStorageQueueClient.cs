namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IStorageQueueClient
{
    Task<StorageQueueMessage?> FetchMessageAsync(CancellationToken cancellationToken);
}
