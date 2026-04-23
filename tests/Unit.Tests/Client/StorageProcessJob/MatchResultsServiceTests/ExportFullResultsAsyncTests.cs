using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace Unit.Tests.Client.StorageProcessJob.MatchResultsServiceTests;

public class ExportFullResultsAsyncTests
{
    private static readonly string FullResultsCsv =
        $"Id,GivenName,FamilyName,SUI_Status,SUI_Score,SUI_NHSNo{Environment.NewLine}"
        + $"1111,Jane,Doe,Match,0.96,92938475748{Environment.NewLine}";

    private static readonly string FullResultsCsvWithErrorRow =
        $"Id,GivenName,FamilyName,SUI_Status,SUI_Score,SUI_NHSNo{Environment.NewLine}"
        + $"1111,Jane,Doe,Match,0.96,92938475748{Environment.NewLine}"
        + $"2222,John,Smith,Error,,{Environment.NewLine}";
    private static readonly MatchResultsBlobNames BlobNames = new(
        "20260120120000_test-file/test-file.csv",
        "20260120120000_test-file/test-file_full-results.csv",
        "20260120120000_test-file/test-file_success.csv"
    );

    private readonly Mock<IBlobStorageClient> _blobStorageClient = new();
    private readonly Mock<ILogger<MatchResultsService>> _logger = new();
    private readonly IMatchResultsService _sut;

    public ExportFullResultsAsyncTests()
    {
        _sut = new MatchResultsService(
            _logger.Object,
            _blobStorageClient.Object,
            Options.Create(
                new StorageProcessJobOptions
                {
                    CsvParserName = CsvParserNameConstants.TypeOne,
                    ProcessedContainerName = "processed",
                }
            )
        );
    }

    [Fact]
    public async Task Should_NotUploadBlobAndInsteadLogWarning_When_NoRecordsProvided()
    {
        var matchedResults = Array.Empty<ProcessedMatchRecord<CsvRecordDto>>();

        await _sut.ExportFullResultsAsync(
            BlobNames,
            "test-file.csv",
            matchedResults,
            CancellationToken.None
        );

        _blobStorageClient.Verify(
            x =>
                x.UploadBlobAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<BinaryData>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );

        _logger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) =>
                            state
                                .ToString()!
                                .Contains("MatchResults is empty", StringComparison.Ordinal)
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_WriteOriginalHeadersAndAppendedValues_When_RecordHasMatchResult()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord(
                new Dictionary<string, string>
                {
                    ["Id"] = "1111",
                    ["GivenName"] = "Jane",
                    ["FamilyName"] = "Doe",
                },
                true,
                MatchStatus.Match,
                0.96m,
                "92938475748"
            ),
        };

        await _sut.ExportFullResultsAsync(
            BlobNames,
            "test-file.csv",
            matchedResults,
            CancellationToken.None
        );

        _blobStorageClient.Verify(
            x =>
                x.UploadBlobAsync(
                    "processed",
                    "20260120120000_test-file/test-file_full-results.csv",
                    It.Is<BinaryData>(data => data.ToString() == FullResultsCsv),
                    "text/csv",
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_WriteErrorStatusAndBlankOptionalFields_When_ResultIsUnavailable()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord(
                new Dictionary<string, string>
                {
                    ["Id"] = "1111",
                    ["GivenName"] = "Jane",
                    ["FamilyName"] = "Doe",
                },
                true,
                MatchStatus.Match,
                0.96m,
                "92938475748"
            ),
            CreateFailedRecord(
                new Dictionary<string, string>
                {
                    ["Id"] = "2222",
                    ["GivenName"] = "John",
                    ["FamilyName"] = "Smith",
                }
            ),
        };

        await _sut.ExportFullResultsAsync(
            BlobNames,
            "test-file.csv",
            matchedResults,
            CancellationToken.None
        );

        _blobStorageClient.Verify(
            x =>
                x.UploadBlobAsync(
                    "processed",
                    "20260120120000_test-file/test-file_full-results.csv",
                    It.Is<BinaryData>(data => data.ToString() == FullResultsCsvWithErrorRow),
                    "text/csv",
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    private static ProcessedMatchRecord<CsvRecordDto> CreateMatchedRecord(
        IReadOnlyDictionary<string, string> originalFields,
        bool isSuccess,
        MatchStatus matchStatus,
        decimal? score,
        string? nhsNumber
    )
    {
        return new ProcessedMatchRecord<CsvRecordDto>
        {
            OriginalData = new CsvRecordDto(new Dictionary<string, string>(originalFields)),
            IsSuccess = isSuccess,
            ApiResult = new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = matchStatus,
                    Score = score,
                    NhsNumber = nhsNumber,
                },
            },
        };
    }

    private static ProcessedMatchRecord<CsvRecordDto> CreateFailedRecord(
        IReadOnlyDictionary<string, string> originalFields
    )
    {
        return new ProcessedMatchRecord<CsvRecordDto>
        {
            OriginalData = new CsvRecordDto(new Dictionary<string, string>(originalFields)),
            IsSuccess = false,
            ErrorMessage = "Matching failed.",
            ApiResult = new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Error,
                    Score = null,
                    NhsNumber = null,
                },
            },
        };
    }
}
