using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace Unit.Tests.StorageProcessFunction;

public class BlobFileOrchestratorTests
{
    private readonly Mock<IBlobStorageClient> _blobFileReader;
    private readonly Mock<IPersonRecordOrchestrator> _blobPayloadProcessor;
    private readonly Mock<IPersonSpecParser<Dictionary<string, string>>> _personSpecParser;
    private readonly BlobFileOrchestrator _sut;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IOptions<StorageProcessFunctionOptions> _options = Options.Create(
        new StorageProcessFunctionOptions
        {
            ProcessedContainerName = "processed",
            CsvParserName = StorageProcessFunctionOptions.CsvParserNameConstants.TypeOne,
        }
    );

    public BlobFileOrchestratorTests()
    {
        _blobFileReader = new Mock<IBlobStorageClient>();
        _blobPayloadProcessor = new Mock<IPersonRecordOrchestrator>();
        _personSpecParser = new Mock<IPersonSpecParser<Dictionary<string, string>>>();
        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
        );
        _sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            _blobPayloadProcessor.Object,
            _personSpecParser.Object,
            _options
        );
    }

    [Fact]
    public async Task Should_ProcessBlob_When_QueueMessageIsValid()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var blobContent = BinaryData.FromString(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );
        var personSpecification = new PersonSpecification
        {
            Given = "Jane",
            Family = "Doe",
            BirthDate = new DateOnly(2012, 5, 10),
        };

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);
        _personSpecParser
            .Setup(x =>
                x.Parse(
                    It.Is<Dictionary<string, string>>(record =>
                        record["GivenName"] == "Jane"
                        && record["FamilyName"] == "Doe"
                        && record["DOB"] == "2012-05-10"
                        && record["Postcode"] == "SW1A 1AA"
                    ),
                    It.Is<HashSet<string>>(headers =>
                        headers.Count == 4
                        && headers.Contains("GivenName")
                        && headers.Contains("FamilyName")
                        && headers.Contains("DOB")
                        && headers.Contains("Postcode")
                    )
                )
            )
            .Returns(personSpecification);
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<List<PersonSpecification>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _blobFileReader.Verify(
            x => x.GetBlobContents(queueMessage, CancellationToken.None),
            Times.Once
        );
        _personSpecParser.Verify(
            x => x.Parse(It.IsAny<Dictionary<string, string>>(), It.IsAny<HashSet<string>>()),
            Times.Once
        );
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.Is<List<PersonSpecification>>(people =>
                        people.Count == 1 && ReferenceEquals(people[0], personSpecification)
                    ),
                    "test-file.csv",
                    CancellationToken.None
                ),
            Times.Once
        );
        var utcNow = $"{_timeProvider.GetUtcNow():yyyyMMddHHmmss}_test-file/test-file.csv";
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    queueMessage,
                    "processed",
                    It.Is<string>(name => name == utcNow),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Throw_When_QueueMessageDoesNotContainContainerName()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(new StorageBlobMessage(null, "test-file.csv"), CancellationToken.None)
        );

        _blobFileReader.Verify(
            x => x.GetBlobContents(It.IsAny<StorageBlobMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<List<PersonSpecification>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    It.IsAny<StorageBlobMessage>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Throw_When_QueueMessageDoesNotContainBlobName()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(new StorageBlobMessage("incoming", null), CancellationToken.None)
        );

        _blobFileReader.Verify(
            x => x.GetBlobContents(It.IsAny<StorageBlobMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<List<PersonSpecification>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    It.IsAny<StorageBlobMessage>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Throw_When_PersonSpecParserThrows()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var blobContent = BinaryData.FromString(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );
        var invalidParser = new Mock<IPersonSpecParser<Dictionary<string, string>>>();
        var sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            _blobPayloadProcessor.Object,
            invalidParser.Object,
            Options.Create(
                new StorageProcessFunctionOptions
                {
                    ProcessedContainerName = "processed",
                    CsvParserName = StorageProcessFunctionOptions.CsvParserNameConstants.TypeOne,
                }
            )
        );

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);
        invalidParser
            .Setup(x =>
                x.Parse(It.IsAny<Dictionary<string, string>>(), It.IsAny<HashSet<string>>())
            )
            .Throws(new InvalidOperationException("Unknown parser type: InvalidParser."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcessAsync(queueMessage, CancellationToken.None)
        );

        Assert.Equal("Unknown parser type: InvalidParser.", exception.Message);
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<List<PersonSpecification>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    It.IsAny<StorageBlobMessage>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }
}
