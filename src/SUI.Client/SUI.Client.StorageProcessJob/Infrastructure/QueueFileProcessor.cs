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
        var processingCompletedSuccessfully = false;
        using var renewalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewalTask = RenewVisibilityUntilCancelledAsync(
            () => currentMessage,
            renewedMessage => currentMessage = renewedMessage,
            () => processingCompletedSuccessfully,
            renewalCts.Token
        );

        try
        {
            // Do we want to delete the message if there is an exception thrown from this?
            // I would say yes, then it can be dealt with manually by checking the logs to see what happened.
            await blobFileOrchestrator.ProcessAsync(blobMessage, cancellationToken);
            processingCompletedSuccessfully = true;
        }
        finally
        {
            await renewalCts.CancelAsync();
            await renewalTask; // Ensure task is finished before moving on
        }

        logger.LogInformation("Finished processing queue message {MessageId}.", message.MessageId);

        // At this point we don't want to stop the message being deleted as it's been processed successfully.
        await queueClient.DeleteMessageAsync(currentMessage, CancellationToken.None);
    }

    private async Task RenewVisibilityUntilCancelledAsync(
        Func<StorageQueueMessage> getMessage,
        Action<StorageQueueMessage> setMessage,
        Func<bool> hasProcessingCompletedSuccessfully,
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
            if (hasProcessingCompletedSuccessfully())
            {
                logger.LogWarning(
                    ex,
                    "Failed to renew queue message visibility after processing completed."
                );
                return;
            }

            logger.LogError(ex, "Failed to renew queue message visibility.");
            throw;
        }
    }
}
