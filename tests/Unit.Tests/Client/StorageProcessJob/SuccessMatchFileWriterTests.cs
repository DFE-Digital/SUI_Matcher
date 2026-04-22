using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace Unit.Tests.Client.StorageProcessJob;

public class SuccessMatchFileWriterTests
{
    private static readonly string SingleRowCsv =
        $"LL ID,Type,NhsNumber{Environment.NewLine}1111,NHSNo,92938475748{Environment.NewLine}";
    private static readonly string SingleRowWithSkippedRowsCsv =
        $"LL ID,Type,NhsNumber{Environment.NewLine}1111,NHSNo,92938475748{Environment.NewLine}";

    private readonly Mock<IBlobStorageClient> _blobStorageClient = new();
    private readonly Mock<ILogger<SuccessMatchFileWriter>> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new(
        new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
    );
    private readonly ISuccessMatchFileWriter _sut;

    public SuccessMatchFileWriterTests()
    {
        _sut = new SuccessMatchFileWriter(
            _timeProvider,
            _logger.Object,
            _blobStorageClient.Object,
            Options.Create(
                new StorageProcessJobOptions
                {
                    CsvParserName = "TypeOne",
                    SuccessContainerName = "success",
                }
            )
        );
    }

    [Fact]
    public async Task Should_WriteOneRow_When_RecordIsHighConfidenceMatch()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord("1111", true, MatchStatus.Match, 0.96m, "92938475748"),
        };

        await _sut.WriteAsync("test-file.csv", matchedResults, CancellationToken.None);

        _blobStorageClient.Verify(
            x =>
                x.UploadBlobAsync(
                    "success",
                    "20260120120000_test-file/test-file_success.csv",
                    It.Is<BinaryData>(data => data.ToString() == SingleRowCsv),
                    "text/csv",
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_NotUploadBlob_When_MatchIsNotHighConfidence()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord("1111", true, MatchStatus.Match, 0.949m, "92938475748"),
            CreateMatchedRecord("2222", true, MatchStatus.PotentialMatch, 0.99m, "92938475749"),
        };

        await _sut.WriteAsync("test-file.csv", matchedResults, CancellationToken.None);

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
    }

    [Fact]
    public async Task Should_NotUploadBlob_When_NoRecordsProvided()
    {
        var matchedResults = Array.Empty<ProcessedMatchRecord<CsvRecordDto>>();

        await _sut.WriteAsync("test-file.csv", matchedResults, CancellationToken.None);

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
    }

    [Fact]
    public async Task Should_NotUploadBlob_When_EligibleRecordIsMissingId()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord(null, true, MatchStatus.Match, 0.96m, "92938475748"),
        };

        await _sut.WriteAsync("test-file.csv", matchedResults, CancellationToken.None);

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

        VerifyWarningLogged("test-file.csv", "Id");
    }

    // Edge case
    [Fact]
    public async Task Should_NotUploadBlob_When_EligibleRecordIsMissingNhsNumber()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord("1111", true, MatchStatus.Match, 0.96m, null),
        };

        await _sut.WriteAsync("test-file.csv", matchedResults, CancellationToken.None);

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

        VerifyWarningLogged("test-file.csv", "NhsNumber");
    }

    [Fact]
    public async Task Should_WriteOnlyValidRows_When_EligibleRowsHaveMissingFields()
    {
        var matchedResults = new[]
        {
            CreateMatchedRecord("1111", true, MatchStatus.Match, 0.96m, "92938475748"),
            CreateMatchedRecord(null, true, MatchStatus.Match, 0.96m, "92938475749"),
            CreateMatchedRecord("3333", true, MatchStatus.Match, 0.96m, null),
        };

        await _sut.WriteAsync("test-file.csv", matchedResults, CancellationToken.None);

        _blobStorageClient.Verify(
            x =>
                x.UploadBlobAsync(
                    "success",
                    "20260120120000_test-file/test-file_success.csv",
                    It.Is<BinaryData>(data => data.ToString() == SingleRowWithSkippedRowsCsv),
                    "text/csv",
                    CancellationToken.None
                ),
            Times.Once
        );

        VerifyWarningLogged("test-file.csv", "Id");
        VerifyWarningLogged("test-file.csv", "NhsNumber");
    }

    private static ProcessedMatchRecord<CsvRecordDto> CreateMatchedRecord(
        string? id,
        bool isSuccess,
        MatchStatus matchStatus,
        decimal? score,
        string? nhsNumber
    )
    {
        var record = new Dictionary<string, string>();

        if (id is not null)
        {
            record["Id"] = id;
        }

        return new ProcessedMatchRecord<CsvRecordDto>
        {
            OriginalData = new CsvRecordDto(record),
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

    private void VerifyWarningLogged(string sourceBlobName, string fieldName)
    {
        _logger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) =>
                            state.ToString()!.Contains(sourceBlobName, StringComparison.Ordinal)
                            && state.ToString()!.Contains(fieldName, StringComparison.Ordinal)
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }
}
