using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Application.UseCases.ReconcilePeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace Unit.Tests.Client.StorageProcessJob.MatchResultsServiceTests;

public class ExportFullResultsAsyncTests
{
    private static readonly string FullResultsCsv =
        $"Id,GivenName,FamilyName,SUI_Status,SUI_Score,SUI_NHSNo,SUI_SearchId{Environment.NewLine}"
        + $"1111,Jane,Doe,Match,0.96,92938475748,search-id-1{Environment.NewLine}";

    private static readonly string FullResultsCsvWithErrorRow =
        $"Id,GivenName,FamilyName,SUI_Status,SUI_Score,SUI_NHSNo,SUI_SearchId{Environment.NewLine}"
        + $"1111,Jane,Doe,Match,0.96,92938475748,search-id-1{Environment.NewLine}"
        + $"2222,John,Smith,Error,-,-,-{Environment.NewLine}";

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
        var csvMatchDataOptions = new CsvMatchDataOptions()
        {
            ColumnMappings = new CsvMatchDataOptions.Headers
            {
                Id = "Id",
                Given = "GivenName",
                Family = "FamilyName",
                BirthDate = "DOB",
                Postcode = "Postcode",
            },
            DateFormat = "yyyy-MM-dd",
        };
        _sut = new MatchResultsService(
            _logger.Object,
            _blobStorageClient.Object,
            Options.Create(new StorageProcessJobOptions { ProcessedContainerName = "processed" }),
            Options.Create(csvMatchDataOptions)
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
                "92938475748",
                "search-id-1"
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
                "92938475748",
                "search-id-1"
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

    [Fact]
    public async Task Should_WriteDerivedFields_When_ReconciliationModeIsEnabled()
    {
        var blobStorageClient = new Mock<IBlobStorageClient>();
        var sut = new MatchResultsService(
            _logger.Object,
            blobStorageClient.Object,
            Options.Create(
                new StorageProcessJobOptions
                {
                    ProcessedContainerName = "processed",
                    ProcessingMode = ProcessingModes.Reconciliation,
                }
            ),
            Options.Create(
                new CsvMatchDataOptions
                {
                    DateFormat = "yyyy-MM-dd",
                    ColumnMappings = new CsvMatchDataOptions.Headers
                    {
                        Id = "Id",
                        Given = "GivenName",
                        Family = "FamilyName",
                        BirthDate = "DOB",
                        Postcode = "Postcode",
                    },
                }
            )
        );
        var record = CreateMatchedRecord(
            new Dictionary<string, string> { ["Id"] = "1111" },
            true,
            MatchStatus.Match,
            0.96m,
            "9999999993",
            "search-id-1"
        );
        record.ReconciliationResult = new ReconciliationResponse
        {
            Status = ReconciliationStatus.NoDifferences,
        };
        record.SourceBirthDate = new DateOnly(2000, 1, 1);
        record.SourceNhsNumber = "9999999993";
        record.AddressComparisonResults = new AddressComparisonResults
        {
            PrimaryAddressSame = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Matched
            ),
            AddressHistoriesIntersect = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Unmatched
            ),
            PrimaryCMSAddressInPDSHistory = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Uncertain,
                AddressComparisonResult.AddressMatchReason.FlatMissing
            ),
            PrimaryPDSAddressInCMSHistory = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Matched
            ),
        };
        var expected =
            $"Id,SUI_Status,SUI_Score,SUI_NHSNo,SUI_SearchId,SUI_AgeGroup,SUI_PrimaryAddressSame,SUI_AddressHistoriesIntersect,SUI_PrimarySourceAddressInPDSHistory,SUI_PrimaryPDSAddressInSourceHistory,SUI_SourceNhsNumberPresent,SUI_SourceNhsNumberEqualsMatchedNhsNumber{Environment.NewLine}"
            + $"1111,NoDifferences,0.96,9999999993,search-id-1,Over 18 years,Matched,Unmatched,Uncertain-FlatMissing,Matched,Yes,Yes{Environment.NewLine}";

        await sut.ExportFullResultsAsync(
            BlobNames,
            "test-file.csv",
            [record],
            CancellationToken.None
        );

        blobStorageClient.Verify(
            client =>
                client.UploadBlobAsync(
                    "processed",
                    BlobNames.FullResultsBlobName,
                    It.Is<BinaryData>(data => data.ToString() == expected),
                    "text/csv",
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_ProcessAndPreserveCompleteGenericSourceSchema()
    {
        var columnMappings = new CsvMatchDataOptions.Headers
        {
            Id = "RecordKey",
            Given = "FirstName",
            Family = "LastName",
            BirthDate = "BirthDate",
            Postcode = "PostalCode",
            Gender = "Sex",
            Email = "Email",
            Phone = "Phone",
            NhsNumber = "SourceHealthNumber",
            Address = "ResidenceHistory",
        };
        var csvOptions = Options.Create(
            new CsvMatchDataOptions
            {
                DateFormat = "yyyy-MM-dd",
                ColumnMappings = columnMappings,
            }
        );
        var originalFields = new Dictionary<string, string>
        {
            [columnMappings.Id] = "record-001",
            [columnMappings.NhsNumber!] = "9999999993",
            [columnMappings.Given] = "Jamie",
            [columnMappings.Family] = "Taylor",
            [columnMappings.Postcode] = "AA1 1AA",
            [columnMappings.BirthDate] = "2000-01-01",
            [columnMappings.Gender] = "female",
            [columnMappings.Address!] =
                "10 Example Road, Exampletown, AA1 1AA; 20 Previous Street, Othertown, BB2 2BB",
        };
        foreach (var index in Enumerable.Range(1, 27))
        {
            originalFields[$"AnalysisField{index:00}"] = $"AnalysisValue{index:00}";
        }
        Assert.Equal(35, originalFields.Count);

        var source = new CsvRecordDto(originalFields);
        var parser = new CsvPersonSpecParser(csvOptions);
        var person = parser.Parse(source);
        var reconciliationData = ((IReconciliationDataParser<CsvRecordDto>)parser).Parse(source);

        Assert.Equal("Jamie", person.Given);
        Assert.Equal("Taylor", person.Family);
        Assert.Equal(new DateOnly(2000, 1, 1), person.BirthDate);
        Assert.Equal("AA1 1AA", person.AddressPostalCode);
        Assert.Equal("female", person.Gender);
        Assert.Equal(27, person.OptionalProperties.Count);
        Assert.All(
            Enumerable.Range(1, 27),
            index =>
                Assert.Equal(
                    $"AnalysisValue{index:00}",
                    person.OptionalProperties[$"AnalysisField{index:00}"]
                )
        );
        Assert.DoesNotContain(columnMappings.NhsNumber!, person.OptionalProperties.Keys);
        Assert.DoesNotContain(columnMappings.Address!, person.OptionalProperties.Keys);
        Assert.Equal("9999999993", reconciliationData.NhsNumber);
        Assert.Equal(originalFields[columnMappings.Address!], reconciliationData.AddressHistory);

        BinaryData? uploadedContent = null;
        var blobStorageClient = new Mock<IBlobStorageClient>();
        blobStorageClient
            .Setup(client =>
                client.UploadBlobAsync(
                    "processed",
                    BlobNames.FullResultsBlobName,
                    It.IsAny<BinaryData>(),
                    "text/csv",
                    CancellationToken.None
                )
            )
            .Callback<string, string, BinaryData, string, CancellationToken>(
                (_, _, content, _, _) => uploadedContent = content
            )
            .Returns(Task.CompletedTask);
        var sut = new MatchResultsService(
            _logger.Object,
            blobStorageClient.Object,
            Options.Create(
                new StorageProcessJobOptions
                {
                    ProcessedContainerName = "processed",
                    ProcessingMode = ProcessingModes.Reconciliation,
                }
            ),
            csvOptions
        );
        var processedRecord = CreateMatchedRecord(
            originalFields,
            true,
            MatchStatus.Match,
            0.96m,
            "9999999993",
            "search-id-1"
        );
        processedRecord.ReconciliationResult = new ReconciliationResponse
        {
            Status = ReconciliationStatus.NoDifferences,
        };
        processedRecord.SourceBirthDate = person.BirthDate;
        processedRecord.SourceNhsNumber = reconciliationData.NhsNumber;
        processedRecord.AddressComparisonResults = new AddressComparisonResults
        {
            PrimaryAddressSame = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Matched
            ),
            AddressHistoriesIntersect = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Matched
            ),
            PrimaryCMSAddressInPDSHistory = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Matched
            ),
            PrimaryPDSAddressInCMSHistory = new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Matched
            ),
        };

        await sut.ExportFullResultsAsync(
            BlobNames,
            "generic-source.csv",
            [processedRecord],
            CancellationToken.None
        );

        Assert.NotNull(uploadedContent);
        using var outputReader = new StringReader(uploadedContent!.ToString());
        var (outputHeaders, outputRecords) = await CsvRecordReader.ReadCsvTextAsync(outputReader);
        var outputRecord = Assert.Single(outputRecords);

        Assert.Equal(46, outputHeaders.Count);
        Assert.All(
            originalFields,
            field => Assert.Equal(field.Value, outputRecord[field.Key])
        );
        Assert.Equal("NoDifferences", outputRecord["SUI_Status"]);
        Assert.Equal("0.96", outputRecord["SUI_Score"]);
        Assert.Equal("9999999993", outputRecord["SUI_NHSNo"]);
        Assert.Equal("search-id-1", outputRecord["SUI_SearchId"]);
        Assert.Equal("Over 18 years", outputRecord["SUI_AgeGroup"]);
        Assert.Equal("Matched", outputRecord["SUI_PrimaryAddressSame"]);
        Assert.Equal("Matched", outputRecord["SUI_AddressHistoriesIntersect"]);
        Assert.Equal("Matched", outputRecord["SUI_PrimarySourceAddressInPDSHistory"]);
        Assert.Equal("Matched", outputRecord["SUI_PrimaryPDSAddressInSourceHistory"]);
        Assert.Equal("Yes", outputRecord["SUI_SourceNhsNumberPresent"]);
        Assert.Equal("Yes", outputRecord["SUI_SourceNhsNumberEqualsMatchedNhsNumber"]);
    }

    private static ProcessedMatchRecord<CsvRecordDto> CreateMatchedRecord(
        IReadOnlyDictionary<string, string> originalFields,
        bool isSuccess,
        MatchStatus matchStatus,
        decimal? score,
        string? nhsNumber,
        string? searchId
    )
    {
        return new ProcessedMatchRecord<CsvRecordDto>
        {
            OriginalData = new CsvRecordDto(new Dictionary<string, string>(originalFields)),
            IsSuccess = isSuccess,
            ApiResult = new PersonMatchResponse
            {
                SearchId = searchId,
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
