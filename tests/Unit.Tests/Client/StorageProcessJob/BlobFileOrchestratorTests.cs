using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace Unit.Tests.Client.StorageProcessJob;

public class BlobFileOrchestratorTests
{
    private const string ValidBlobContent = """
        Id,GivenName,FamilyName,DOB,Postcode
        1111,Jane,Doe,2012-05-10,SW1A 1AA
        """;
    private const string ValidBlobContentWithOptionalHeaders = """
        Id,GivenName,FamilyName,DOB,Postcode,Email,Gender,Phone
        1111,Jane,Doe,2012-05-10,SW1A 1AA,jane@example.com,F,07123456789
        """;
    private static readonly MatchResultsBlobNames BlobNames = new(
        "20260120120000_test-file/test-file.csv",
        "20260120120000_test-file/test-file_full-results.csv",
        "20260120120000_test-file/test-file_success.csv"
    );
    private readonly Mock<IBlobStorageClient> _blobFileReader;
    private readonly Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>> _blobPayloadProcessor;
    private readonly Mock<ILogger<BlobFileOrchestrator>> _logger;
    private readonly MatchResultsBlobNameBuilder _matchResultsBlobNameBuilder;
    private readonly Mock<IMatchResultsService> _matchResultsService;
    private readonly BlobFileOrchestrator _sut;
    private readonly CsvMatchingHeadersProvider _matchingHeadersProvider;
    private readonly IOptions<StorageProcessJobOptions> _options = Options.Create(
        new StorageProcessJobOptions
        {
            ProcessedContainerName = "processed",
            SuccessContainerName = "success",
            CsvParserName = CsvParserNameConstants.TypeOne,
        }
    );

    public BlobFileOrchestratorTests()
    {
        _blobFileReader = new Mock<IBlobStorageClient>();
        _blobPayloadProcessor = new Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>>();
        _logger = new Mock<ILogger<BlobFileOrchestrator>>();
        _matchResultsBlobNameBuilder = new MatchResultsBlobNameBuilder(
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero))
        );
        _matchResultsService = new Mock<IMatchResultsService>();
        _matchingHeadersProvider = CreateRequiredHeadersProvider();
        _sut = CreateSut();
    }

    [Fact]
    public async Task Should_ProcessBlob_When_QueueMessageIsValid()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        IEnumerable<CsvRecordDto>? capturedRecords = null;

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .Callback<IEnumerable<CsvRecordDto>, string, CancellationToken>(
                (records, _, _) => capturedRecords = records.ToList()
            )
            .ReturnsAsync(CreateMatchedResults());

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _blobFileReader.Verify(
            x => x.GetBlobContents(queueMessage, CancellationToken.None),
            Times.Once
        );

        Assert.NotNull(capturedRecords);
        var recordsList = capturedRecords.ToList();
        var record = Assert.Single(recordsList);
        Assert.Equal("1111", record.Record["Id"]);
        Assert.Equal("Jane", record.Record["GivenName"]);
        Assert.Equal("Doe", record.Record["FamilyName"]);
        Assert.Equal("2012-05-10", record.Record["DOB"]);
        Assert.Equal("SW1A 1AA", record.Record["Postcode"]);

        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                ),
            Times.Once
        );
        _blobFileReader.Verify(
            x =>
                x.ArchiveProcessedAsync(
                    queueMessage,
                    "processed",
                    It.Is<string>(name => name == "20260120120000_test-file/test-file.csv"),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_CallSuccessWriter_When_ThereAreSuccessfulMatches()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(CreateMatchedResults());

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _blobFileReader.Verify(
            x => x.GetBlobContents(queueMessage, CancellationToken.None),
            Times.Once
        );
        _matchResultsService.Verify(
            x =>
                x.ExportSuccessResultsAsync(
                    It.Is<MatchResultsBlobNames>(names => names == BlobNames),
                    "test-file.csv",
                    It.Is<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(records =>
                        records.Count == 1
                        && records.Single().OriginalData.Record["Id"] == "1111"
                        && records.Single().ApiResult!.Result!.NhsNumber == "92938475748"
                    ),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_CallFullResultsWriter_When_RecordsAreProcessed()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var matchedResults = CreateMatchedResults();

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(matchedResults);

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _matchResultsService.Verify(
            x =>
                x.ExportFullResultsAsync(
                    It.Is<MatchResultsBlobNames>(names => names == BlobNames),
                    "test-file.csv",
                    It.Is<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(records =>
                        records.Count == 1 && records.Single().OriginalData.Record["Id"] == "1111"
                    ),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_LogWarning_When_OptionalHeadersAreMissing()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(CreateMatchedResults());

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        VerifyWarningLogged("test-file.csv", "Email");
        VerifyWarningLogged("test-file.csv", "Gender");
        VerifyWarningLogged("test-file.csv", "Phone");
    }

    [Fact]
    public async Task Should_NotLogWarning_When_OptionalHeadersArePresent()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContentWithOptionalHeaders));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(CreateMatchedResults());

        await _sut.ProcessAsync(queueMessage, CancellationToken.None);

        _logger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never
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
        VerifyNoProcessingSideEffects();
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
        VerifyNoProcessingSideEffects();
    }

    [Fact]
    public async Task Should_Throw_When_PersonSpecParserThrows()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var failingOrchestrator = new Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>>();
        var sut = new BlobFileOrchestrator(
            NullLogger<BlobFileOrchestrator>.Instance,
            _blobFileReader.Object,
            failingOrchestrator.Object,
            _matchResultsService.Object,
            _matchResultsBlobNameBuilder,
            _matchingHeadersProvider,
            Options.Create(
                new StorageProcessJobOptions
                {
                    ProcessedContainerName = "processed",
                    SuccessContainerName = "success",
                    CsvParserName = CsvParserNameConstants.TypeOne,
                }
            )
        );

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
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
        VerifyNoProcessingSideEffects();
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

        Assert.Equal("CSV is missing required headers: Id, Postcode.", exception.Message);
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _matchResultsService.Verify(
            x =>
                x.ExportSuccessResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _matchResultsService.Verify(
            x =>
                x.ExportFullResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(),
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
    public async Task Should_NotArchiveBlob_When_SuccessMatchWriterThrows()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var matchedResults = CreateMatchedResults();

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(matchedResults);
        _matchResultsService
            .Setup(x =>
                x.ExportSuccessResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    "test-file.csv",
                    matchedResults,
                    CancellationToken.None
                )
            )
            .ThrowsAsync(new InvalidOperationException("Could not write success file."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(queueMessage, CancellationToken.None)
        );

        Assert.Equal("Could not write success file.", exception.Message);
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
        _matchResultsService.Verify(
            x =>
                x.ExportFullResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_NotArchiveBlob_When_FullMatchResultsWriterThrows()
    {
        var queueMessage = new StorageBlobMessage("incoming", "test-file.csv");
        var matchedResults = CreateMatchedResults();

        _blobFileReader
            .Setup(x => x.GetBlobContents(queueMessage, CancellationToken.None))
            .ReturnsAsync(BinaryData.FromString(ValidBlobContent));
        _blobPayloadProcessor
            .Setup(x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    "test-file.csv",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(matchedResults);
        _matchResultsService
            .Setup(x =>
                x.ExportFullResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    "test-file.csv",
                    matchedResults,
                    CancellationToken.None
                )
            )
            .ThrowsAsync(new InvalidOperationException("Could not write full match results file."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessAsync(queueMessage, CancellationToken.None)
        );

        Assert.Equal("Could not write full match results file.", exception.Message);
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

    private static List<ProcessedMatchRecord<CsvRecordDto>> CreateMatchedResults()
    {
        return
        [
            new()
            {
                OriginalData = new CsvRecordDto(
                    new Dictionary<string, string> { ["Id"] = "1111", ["GivenName"] = "Jane" }
                ),
                IsSuccess = true,
                ApiResult = new PersonMatchResponse
                {
                    Result = new MatchResult
                    {
                        MatchStatus = MatchStatus.Match,
                        Score = 0.96m,
                        NhsNumber = "92938475748",
                    },
                },
            },
        ];
    }

    private BlobFileOrchestrator CreateSut()
    {
        return new BlobFileOrchestrator(
            _logger.Object,
            _blobFileReader.Object,
            _blobPayloadProcessor.Object,
            _matchResultsService.Object,
            _matchResultsBlobNameBuilder,
            _matchingHeadersProvider,
            _options
        );
    }

    private static CsvMatchingHeadersProvider CreateRequiredHeadersProvider(
        string id = "Id",
        string given = "GivenName",
        string family = "FamilyName",
        string birthDate = "DOB",
        string postcode = "Postcode",
        string email = "Email",
        string gender = "Gender",
        string phone = "Phone"
    )
    {
        return new CsvMatchingHeadersProvider(
            Options.Create(
                new CsvMatchDataOptions
                {
                    DateFormat = "yyyy-MM-dd",
                    ColumnMappings = new CsvMatchDataOptions.Headers
                    {
                        Id = id,
                        Given = given,
                        Family = family,
                        BirthDate = birthDate,
                        Postcode = postcode,
                        Email = email,
                        Gender = gender,
                        Phone = phone,
                    },
                }
            )
        );
    }

    private void VerifyNoProcessingSideEffects()
    {
        _blobPayloadProcessor.Verify(
            x =>
                x.ProcessAsync(
                    It.IsAny<IEnumerable<CsvRecordDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _matchResultsService.Verify(
            x =>
                x.ExportSuccessResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _matchResultsService.Verify(
            x =>
                x.ExportFullResultsAsync(
                    It.IsAny<MatchResultsBlobNames>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>>>(),
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

    private void VerifyWarningLogged(string sourceBlobName, string missingHeader)
    {
        _logger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) =>
                            state.ToString()!.Contains(sourceBlobName, StringComparison.Ordinal)
                            && state.ToString()!.Contains(missingHeader, StringComparison.Ordinal)
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
