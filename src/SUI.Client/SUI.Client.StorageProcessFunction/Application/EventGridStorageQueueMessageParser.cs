using System.Text.Json;

namespace SUI.StorageProcessFunction.Application;

public sealed class EventGridStorageQueueMessageParser : IStorageQueueMessageParser
{
    private const string BlobCreatedEventType = "Microsoft.Storage.BlobCreated";
    private const string IncomingContainerName = "incoming";
    private const string ContainerMarker = "/containers/";
    private const string BlobMarker = "/blobs/";

    private sealed record QueueEventPayload(string? EventType, string? Subject);

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

    private static QueueEventPayload Deserialize(string queueMessage)
    {
        try
        {
            using var document = JsonDocument.Parse(queueMessage);
            var root = document.RootElement;
            var events = root.ValueKind switch
            {
                JsonValueKind.Array => root,
                JsonValueKind.Object => BuildSingleEventArray(root),
                _ => throw new InvalidStorageQueueMessageException(
                    "Queue message was not valid Event Grid JSON."
                ),
            };

            return events.GetArrayLength() switch
            {
                0 => throw new InvalidStorageQueueMessageException(
                    "Queue message did not contain any events."
                ),
                > 1 => throw new InvalidStorageQueueMessageException(
                    "Queue message contained multiple events. Exactly one event is expected."
                ),
                _ => ReadEvent(events[0]),
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

    private static JsonElement BuildSingleEventArray(JsonElement element)
    {
        using var document = JsonDocument.Parse($"[{element.GetRawText()}]");
        return document.RootElement.Clone();
    }

    private static QueueEventPayload ReadEvent(JsonElement element)
    {
        var eventType =
            element.TryGetProperty("eventType", out var eventTypeProperty)
            && eventTypeProperty.ValueKind == JsonValueKind.String
                ? eventTypeProperty.GetString()
                : null;

        var subject =
            element.TryGetProperty("subject", out var subjectProperty)
            && subjectProperty.ValueKind == JsonValueKind.String
                ? subjectProperty.GetString()
                : null;

        return new QueueEventPayload(eventType, subject);
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

        return new StorageBlobMessage
        {
            ContainerName = decodedContainerName,
            BlobName = Uri.UnescapeDataString(blobName),
        };
    }
}
