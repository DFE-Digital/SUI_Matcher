using Microsoft.Extensions.Logging;

namespace Shared.Util;

public static class RetryUtil
{
    public static async Task RetryAsync(Func<Task> action, int retryCount, int delayMs, ILogger logger)
    {
        int attempts = 0;
        while (attempts < retryCount)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                attempts++;
                logger.LogInformation(ex, "Retry attempt {Attempts} failed: {Message}. Retrying in {DelayMs}ms.", attempts, ex.Message, delayMs);
                
                if (attempts >= retryCount)
                    throw;

                await Task.Delay(delayMs);
            }
        }
    }
    
}