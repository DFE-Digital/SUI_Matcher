using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace Unit.Tests.StorageProcessFunction;

public class BlobFileOrchestratorTests
{
    private readonly Mock<IBlobStorageClient> _blobFileReader;
    private readonly Mock<IPersonRecordCsvParserFactory> _personSpecificationCsvParserFactory;
    private readonly Mock<IPersonSpecificationCsvParser> _personSpecificationCsvParser;
    private readonly Mock<IPersonRecordOrchestrator> _blobPayloadProcessor;
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
        _personSpecificationCsvParserFactory = new Mock<IPersonRecordCsvParserFactory>();
        _personSpecificationCsvParser = new Mock<IPersonSpecificationCsvParser>();
        _blobPayloadProcessor = new Mock<IPersonRecordOrchestrator>();
        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
        );
        _sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            _personSpecificationCsvParserFactory.Object,
            _blobPayloadProcessor.Object,
            _options
        );
    }

    [Fact]
    public async Task Should_ProcessBlob_When_QueueMessageIsValid()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var blobContent = BinaryData.FromString("test");
        var personSpecifications = new List<PersonSpecification>
        {
            new()
            {
                Given = "Jane",
                Family = "Doe",
                BirthDate = new DateOnly(2012, 5, 10),
            },
        };

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);
        _personSpecificationCsvParserFactory
            .Setup(x => x.Create(StorageProcessFunctionOptions.CsvParserNameConstants.TypeOne))
            .Returns(_personSpecificationCsvParser.Object);
        _personSpecificationCsvParser
            .Setup(x => x.ParseListAsync(blobContent, "test-file.csv", CancellationToken.None))
            .Returns(personSpecifications);
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
        _personSpecificationCsvParserFactory.Verify(
            x => x.Create(StorageProcessFunctionOptions.CsvParserNameConstants.TypeOne),
            Times.Once
        );
        _personSpecificationCsvParser.Verify(
            x => x.ParseListAsync(blobContent, "test-file.csv", CancellationToken.None),
            Times.Once
        );
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.Is<List<PersonSpecification>>(people =>
                        ReferenceEquals(people, personSpecifications)
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
    public async Task Should_Throw_When_CsvParserNameIsInvalid()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var blobContent = BinaryData.FromString("test");
        var sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _timeProvider,
            _blobFileReader.Object,
            new PersonRecordCsvParserFactory(),
            _blobPayloadProcessor.Object,
            Options.Create(
                new StorageProcessFunctionOptions
                {
                    ProcessedContainerName = "processed",
                    CsvParserName = "InvalidParser",
                }
            )
        );

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(blobContent);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcessAsync(queueMessage, CancellationToken.None)
        );

        Assert.Equal("Unknown parser type.", exception.Message);
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
