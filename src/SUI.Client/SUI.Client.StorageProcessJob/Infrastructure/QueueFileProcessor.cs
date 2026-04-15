using Microsoft.Extensions.Logging;

namespace SUI.Client.StorageProcessJob.Infrastructure;

public class QueueFileProcessor(ILogger<QueueFileProcessor> logger)
{
    public async Task RunAsync()
    {
        logger.LogInformation("Running Storage Process Job.");
    }
}
