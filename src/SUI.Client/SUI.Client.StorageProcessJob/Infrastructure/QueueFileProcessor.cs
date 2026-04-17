using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Infrastructure;

public class QueueFileProcessor(
    ILogger<QueueFileProcessor> logger,
    TimeProvider timeProvider,
    IStorageQueueClient queueClient,
    IStorageQueueMessageParser messageParser,
    IBlobFileOrchestrator blobFileOrchestrator,
    IOptions<StorageProcessJobOptions> options
)
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

        var blobMessage = messageParser.Parse(message.MessageText);
        var currentMessage = message;
        using var renewalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewalTask = RenewVisibilityUntilCancelledAsync(
            () => currentMessage,
            renewedMessage => currentMessage = renewedMessage,
            renewalCts.Token
        );

        try
        {
            await blobFileOrchestrator.ProcessAsync(blobMessage, cancellationToken);
        }
        finally
        {
            await renewalCts.CancelAsync();
            await IgnoreCancellationAsync(renewalTask); // Ensure task is finished before moving on
        }

        logger.LogInformation("Finished processing queue message {MessageId}.", message.MessageId);

        // At this point we don't want to stop the message being deleted as it's been processed successfully.
        await queueClient.DeleteMessageAsync(currentMessage, CancellationToken.None);
    }

    private async Task RenewVisibilityUntilCancelledAsync(
        Func<StorageQueueMessage> getMessage,
        Action<StorageQueueMessage> setMessage,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var visibilityTimeout = TimeSpan.FromMinutes(
                options.Value.MessageVisibilityTimeoutMinutes
            );
            using var timer = new PeriodicTimer(
                TimeSpan.FromMinutes(options.Value.MessageVisibilityRenewalIntervalMinutes),
                timeProvider
            );

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var renewedMessage = await queueClient.RenewMessageVisibilityAsync(
                    getMessage(),
                    visibilityTimeout,
                    cancellationToken
                );

                setMessage(renewedMessage);

                logger.LogDebug(
                    "Renewed queue message visibility for message {MessageId}.",
                    renewedMessage.MessageId
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to renew queue message visibility.");
            throw;
        }
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected when processing finishes or the job is shutting down.
        }
    }
}
