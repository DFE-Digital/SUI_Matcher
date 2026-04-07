using Azure.Messaging.EventGrid;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;
using SUI.StorageProcessFunction.Exceptions;

namespace SUI.StorageProcessFunction.Infrastructure.Azure;

public sealed class EventGridStorageQueueMessageParser : IStorageQueueMessageParser
{
    private const string BlobCreatedEventType = "Microsoft.Storage.BlobCreated";
    private const string IncomingContainerName = "incoming";
    private const string ContainerMarker = "/containers/";
    private const string BlobMarker = "/blobs/";

    public StorageBlobMessage Parse(string queueMessage)
    {
        if (string.IsNullOrWhiteSpace(queueMessage))
        {
            throw new InvalidStorageQueueMessageException("Queue message was empty.");
        }

        var eventGridEvent = Deserialize(queueMessage);

        if (
            !string.Equals(eventGridEvent.EventType, BlobCreatedEventType, StringComparison.Ordinal)
        )
        {
            throw new InvalidStorageQueueMessageException(
                $"Queue message eventType '{eventGridEvent.EventType}' is not supported."
            );
        }

        if (string.IsNullOrWhiteSpace(eventGridEvent.Subject))
        {
            throw new InvalidStorageQueueMessageException("Queue message did not contain subject.");
        }

        return ParseSubject(eventGridEvent.Subject);
    }

    private static EventGridEvent Deserialize(string queueMessage)
    {
        try
        {
            var events = EventGridEvent.ParseMany(BinaryData.FromString(queueMessage));

            return events.Length switch
            {
                0 => throw new InvalidStorageQueueMessageException(
                    "Queue message did not contain any events."
                ),
                > 1 => throw new InvalidStorageQueueMessageException(
                    "Queue message contained multiple events. Exactly one event is expected."
                ),
                _ => events[0],
            };
        }
        catch (InvalidStorageQueueMessageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidStorageQueueMessageException(
                "Queue message was not valid Event Grid JSON.",
                ex
            );
        }
    }

    private static StorageBlobMessage ParseSubject(string subject)
    {
        var containerStart = subject.IndexOf(ContainerMarker, StringComparison.Ordinal);
        if (containerStart < 0)
        {
            throw new InvalidStorageQueueMessageException(
                "Queue message subject did not contain a container."
            );
        }

        var blobStart = subject.IndexOf(BlobMarker, StringComparison.Ordinal);
        if (blobStart < 0 || blobStart <= containerStart + ContainerMarker.Length)
        {
            throw new InvalidStorageQueueMessageException(
                "Queue message subject did not contain a blob path."
            );
        }

        var containerName = subject.Substring(
            containerStart + ContainerMarker.Length,
            blobStart - (containerStart + ContainerMarker.Length)
        );
        var blobName = subject[(blobStart + BlobMarker.Length)..];

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidStorageQueueMessageException(
                "Queue message subject did not contain a container."
            );
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new InvalidStorageQueueMessageException(
                "Queue message subject did not contain a blob path."
            );
        }

        var decodedContainerName = Uri.UnescapeDataString(containerName);
        if (!string.Equals(decodedContainerName, IncomingContainerName, StringComparison.Ordinal))
        {
            throw new InvalidStorageQueueMessageException(
                $"Queue message container '{decodedContainerName}' is not supported."
            );
        }

        return new StorageBlobMessage(decodedContainerName, Uri.UnescapeDataString(blobName));
    }
}
