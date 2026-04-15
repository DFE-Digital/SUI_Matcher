using Microsoft.Extensions.Logging;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Infrastructure;

public class QueueFileProcessor(ILogger<QueueFileProcessor> logger, IStorageQueueClient queueClient)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running Storage Process Job.");
        var message = await queueClient.FetchMessageAsync(cancellationToken);

        if (message is null)
        {
            logger.LogInformation("No messages in queue.");
            return;
        }

        logger.LogInformation("Found messages in queue.");
    }
}
