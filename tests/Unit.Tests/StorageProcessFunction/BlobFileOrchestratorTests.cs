using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace Unit.Tests.StorageProcessFunction;

public class BlobFileOrchestratorTests
{
    private readonly Mock<IBlobStorageClient> _blobFileReader;
    private readonly Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>> _blobPayloadProcessor;
    private readonly BlobFileOrchestrator _sut;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IOptions<StorageProcessFunctionOptions> _options = Options.Create(
        new StorageProcessFunctionOptions
        {
            ProcessedContainerName = "processed",
            CsvParserName = CsvParserNameConstants.TypeOne,
        }
    );

    public BlobFileOrchestratorTests()
    {
        _blobFileReader = new Mock<IBlobStorageClient>();
        _blobPayloadProcessor = new Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>>();
        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
        );
        _sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            _blobPayloadProcessor.Object,
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
        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync([]);

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _blobFileReader.Verify(
            x => x.GetBlobContents(queueMessage, CancellationToken.None),
            Times.Once
        );
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.Is<IEnumerable<CsvRecordDto>>(records =>
                        records.Count() == 1
                        && records.First().Record["GivenName"] == "Jane"
                        && records.First().Record["FamilyName"] == "Doe"
                        && records.First().Record["DOB"] == "2012-05-10"
                        && records.First().Record["Postcode"] == "SW1A 1AA"
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
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
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
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
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
        var failingOrchestrator = new Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>>();
        var sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            failingOrchestrator.Object,
            Options.Create(
                new StorageProcessFunctionOptions
                {
                    ProcessedContainerName = "processed",
                    CsvParserName = CsvParserNameConstants.TypeOne,
                }
            )
        );

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);
        failingOrchestrator
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Throws(new InvalidOperationException("Unknown parser type: InvalidParser."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcessAsync(queueMessage, CancellationToken.None)
        );

        Assert.Equal("Unknown parser type: InvalidParser.", exception.Message);
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
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
    public async Task Should_Throw_When_HeaderValidationFails()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var blobContent = BinaryData.FromString(
            """
            GivenName,FamilyName,DOB
            Jane,Doe,2012-05-10
            """
        );

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(queueMessage, CancellationToken.None)
        );

        Assert.Equal("CSV is missing required headers: Postcode.", exception.Message);
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
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
