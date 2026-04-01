using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction;

public class EventGridStorageQueueMessageParserTests
{
    private readonly EventGridStorageQueueMessageParser _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Throw_When_QueueMessageIsNullOrEmpty(string? queueMessage)
    {
        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage!)
        );

        Assert.Equal("Queue message was empty.", exception.Message);
    }

    [Fact]
    public void Should_ParseBlobMessage_When_QueueMessageContainsSingleEventArray()
    {
        var queueMessage = BuildQueueMessage(
            subject: "/blobServices/default/containers/incoming/blobs/test-file.csv"
        );

        var result = _sut.Parse(queueMessage);

        Assert.Equal("incoming", result.ContainerName);
        Assert.Equal("test-file.csv", result.BlobName);
    }

    // Test to show we are guarding against incorrect EventGrid configurations
    [Fact]
    public void Should_Throw_When_ContainerIsNotIncoming()
    {
        var queueMessage = BuildQueueMessage(
            subject: "/blobServices/default/containers/processed/blobs/test-file.csv",
            asArray: false
        );

        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage)
        );

        Assert.Equal("Queue message container 'processed' is not supported.", exception.Message);
    }

    // Test to show we are guarding against incorrect EventGrid configurations
    [Fact]
    public void Should_Throw_When_EventTypeIsNotBlobCreated()
    {
        var queueMessage = BuildQueueMessage(
            subject: "/blobServices/default/containers/incoming/blobs/test-file.csv",
            eventType: "Microsoft.Storage.BlobDeleted",
            asArray: false
        );

        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage)
        );

        Assert.Equal(
            "Queue message eventType 'Microsoft.Storage.BlobDeleted' is not supported.",
            exception.Message
        );
    }

    [Fact]
    public void Should_Throw_When_QueueMessageContainsMultipleEvents_BecauseWeOnlyExpectOneEventPerFile()
    {
        var queueMessage = BuildQueueMessage([
            BuildEvent("/blobServices/default/containers/incoming/blobs/test-file.csv"),
            BuildEvent(
                "/blobServices/default/containers/incoming/blobs/test-file-2.csv",
                id: "55555555-5555-5555-5555-555555555555",
                eventTime: "2026-04-01T12:01:00Z"
            ),
        ]);

        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage)
        );

        Assert.Equal(
            "Queue message contained multiple events. Exactly one event is expected.",
            exception.Message
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Throw_When_SubjectIsEmptyOrWhitespace(string? subject)
    {
        var queueMessage = BuildQueueMessage(BuildEvent(subject), asArray: false);

        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage)
        );

        Assert.Equal("Queue message did not contain subject.", exception.Message);
    }

    [Fact]
    public void Should_Throw_When_SubjectIsMissing()
    {
        var queueMessage = BuildQueueMessage(
            BuildEvent(subject: null, omitSubject: true),
            asArray: false
        );

        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage)
        );

        Assert.Equal("Queue message was not valid Event Grid JSON.", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Should_Throw_When_QueueMessageDeserializationThrowsUnexpectedException()
    {
        const string queueMessage = "{";

        var exception = Assert.Throws<InvalidStorageQueueMessageException>(() =>
            _sut.Parse(queueMessage)
        );

        Assert.Equal("Queue message was not valid Event Grid JSON.", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    private static string BuildQueueMessage(
        string subject,
        string eventType = "Microsoft.Storage.BlobCreated",
        bool asArray = true
    ) => BuildQueueMessage(BuildEvent(subject, eventType: eventType), asArray);

    private static string BuildQueueMessage(string[] events) =>
        "[\n" + string.Join(",\n", events) + "\n]";

    private static string BuildQueueMessage(string eventPayload, bool asArray) =>
        asArray ? BuildQueueMessage([eventPayload]) : eventPayload;

    private static string BuildEvent(
        string? subject,
        string eventType = "Microsoft.Storage.BlobCreated",
        string id = "11111111-1111-1111-1111-111111111111",
        string eventTime = "2026-04-01T12:00:00Z",
        bool omitSubject = false
    ) =>
        $$"""
            {
              "id": "{{id}}",
            {{FormatSubjectProperty(subject, omitSubject)}}
              "eventType": "{{eventType}}",
              "eventTime": "{{eventTime}}",
              "data": {},
              "dataVersion": "1",
              "metadataVersion": "1"
            }
            """;

    private static string FormatSubjectProperty(string? value, bool omitSubject) =>
        omitSubject ? string.Empty : $"  \"subject\": \"{value}\",";
}
