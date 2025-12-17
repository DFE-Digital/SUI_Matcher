using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Models;

using SUI.Client.Core;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Infrastructure.FileSystem;

namespace Unit.Tests.Client;

public class ProcessCsVFileAsyncTests : IDisposable
{
    private readonly List<string> _testFiles = [];

    [Fact]
    public async Task ProcessCsvFileAsync_PassesExistingAllOptionalFieldsToMatchPersonAsync()
    {
        var mockLogger = new Mock<ILogger<MatchingCsvFileProcessor>>();
        var mockApi = new Mock<IMatchingService>();
        var mapping = new CsvMappingConfig
        {
            /* set up mappings as needed */
        };
        var watcherConfig = Options.Create(new CsvWatcherConfig { EnableGenderSearch = true });

        MatchPersonPayload? capturedPayload = null;
        mockApi.Setup(x => x.MatchPersonAsync(It.IsAny<MatchPersonPayload>()))
            .Callback<MatchPersonPayload>(p => capturedPayload = p)
            .ReturnsAsync(new PersonMatchResponse());

        var processor = new MatchingCsvFileProcessor(mockLogger.Object, mapping, mockApi.Object, watcherConfig);

        // Prepare test CSV file with all optional fields
        var tempDir = Path.GetTempPath();
        var outputPath = tempDir;
        var headers = new HashSet<string>
        {
            "Given",
            "Family",
            "ActiveCIN",
            "ActiveCLA",
            "ActiveCP",
            "ActiveEHM",
            "Ethnicity",
            "ImmigrationStatus",
            "AddressHistory"
        };
        var records = new List<Dictionary<string, string>>
        {
            new()
            {
                ["Given"] = "Jane",
                ["Family"] = "Doe",
                ["ActiveCIN"] = "CIN123",
                ["ActiveCLA"] = "CLA456",
                ["ActiveCP"] = "CP789",
                ["ActiveEHM"] = "EHM321",
                ["Ethnicity"] = "A1 - White-British",
                ["ImmigrationStatus"] = "Settled",
            }
        };

        await processor.ProcessCsvFileAsync("test", headers, records, outputPath);

        Assert.NotNull(capturedPayload);
        Assert.Equal("CIN123", capturedPayload!.OptionalProperties["ActiveCIN"]);
        Assert.Equal("CLA456", capturedPayload.OptionalProperties["ActiveCLA"]);
        Assert.Equal("CP789", capturedPayload.OptionalProperties["ActiveCP"]);
        Assert.Equal("EHM321", capturedPayload.OptionalProperties["ActiveEHM"]);
        Assert.Equal("A1 - White-British", capturedPayload.OptionalProperties["Ethnicity"]);
        Assert.Equal("Settled", capturedPayload.OptionalProperties["ImmigrationStatus"]);
    }

    [Fact]
    public async Task ProcessCsvFileAsync_ShouldOutputSuccessfulMatchedResults_ToSuccessOutputDirectory()
    {
        var mockLogger = new Mock<ILogger<MatchingCsvFileProcessor>>();
        var mockApi = new Mock<IMatchingService>();
        var mapping = new CsvMappingConfig
        {
            /* set up mappings as needed */
        };
        var tempDir = Path.GetTempPath();
        var watcherConfig =
            Options.Create(new CsvWatcherConfig() { MatchedRecordsDirectory = $"{tempDir}/Processed/Matched" });

        mockApi.SetupSequence(x => x.MatchPersonAsync(It.IsAny<MatchPersonPayload>()))
            .ReturnsAsync(new PersonMatchResponse()
            {
                Result = new MatchResult() { MatchStatus = MatchStatus.Match, NhsNumber = "123467890" }
            })
            .ReturnsAsync(new PersonMatchResponse()
            {
                Result = new MatchResult() { MatchStatus = MatchStatus.PotentialMatch, NhsNumber = "111111111" }
            })
            .ReturnsAsync(new PersonMatchResponse()
            {
                Result = new MatchResult() { MatchStatus = MatchStatus.ManyMatch }
            });

        var processor = new MatchingCsvFileProcessor(mockLogger.Object, mapping, mockApi.Object, watcherConfig);

        var outputPath = tempDir;
        var headers = new HashSet<string>
        {
            "Id",
            "Given",
            "Family",
            "DOB"
        };
        var dob = DateTime.Now.AddYears(-10).ToString(MatchingCsvFileProcessor.AcceptedCsvDateFormats.First());
        var records = new List<Dictionary<string, string>>
        {
            new()
            {
                ["Id"] = "L1",
                ["Given"] = "John",
                ["Family"] = "Smith",
                ["DOB"] = dob
            },
            new()
            {
                ["Id"] = "L2",
                ["Given"] = "Jane",
                ["Family"] = "Doe",
                ["DOB"] = dob
            },
            new()
            {
                ["Id"] = "L3",
                ["Given"] = "Jim",
                ["Family"] = "Beam",
                ["DOB"] = dob
            }
        };

        await processor.ProcessCsvFileAsync("test", headers, records, outputPath);

        // Assert
        var files = Directory.EnumerateFiles($"{tempDir}/Processed/Matched", "*.csv").ToList();
        _testFiles.AddRange(files);
        Assert.Single(_testFiles);

        var content = await File.ReadAllLinesAsync(_testFiles.First());

        Assert.Equal(2, content.Length); // Header + 1 record with Match

        var contentRowSplit = content[1].Split(",");
        Assert.Contains("123467890", contentRowSplit.Last()); // First and only record should have NhsNumber
        Assert.Equal("L1", contentRowSplit.First()); // First and only record should have NhsNumber
    }

    [Fact]
    public async Task ProcessCsvFileAsync_ShouldOutputSuccessfulMatchedResults_ToSuccessOutputDirectory_WhenOnlyAChildAgeGroup()
    {
        var mockLogger = new Mock<ILogger<MatchingCsvFileProcessor>>();
        var mockApi = new Mock<IMatchingService>();
        var mapping = new CsvMappingConfig
        {
            /* set up mappings as needed */
        };
        var tempDir = Path.GetTempPath();
        var watcherConfig =
            Options.Create(new CsvWatcherConfig() { MatchedRecordsDirectory = $"{tempDir}/Processed/Matched" });

        mockApi.SetupSequence(x => x.MatchPersonAsync(It.IsAny<MatchPersonPayload>()))
            .ReturnsAsync(new PersonMatchResponse()
            {
                Result = new MatchResult() { MatchStatus = MatchStatus.Match, NhsNumber = "123467890" }
            })
            .ReturnsAsync(new PersonMatchResponse()
            {
                Result = new MatchResult() { MatchStatus = MatchStatus.Match, NhsNumber = "0987654321" }
            })
            .ReturnsAsync(new PersonMatchResponse()
            {
                Result = new MatchResult() { MatchStatus = MatchStatus.Match, NhsNumber = "1029384756" }
            });

        var processor = new MatchingCsvFileProcessor(mockLogger.Object, mapping, mockApi.Object, watcherConfig);


        var outputPath = tempDir;
        var headers = new HashSet<string>
        {
            "DOB",
            "Id",
            "Given",
            "Family"
        };
        var records = new List<Dictionary<string, string>>
        {
            new()
            {
                ["Id"] = "L1",
                ["Given"] = "John",
                ["Family"] = "Smith",
                ["DOB"] = DateTime.Now.AddYears(-19).ToString(MatchingCsvFileProcessor.AcceptedCsvDateFormats.First()) // Not Child age
            },
            new()
            {
                ["Id"] = "L2",
                ["Given"] = "Jane",
                ["Family"] = "Doe",
                ["DOB"] = DateTime.Now.AddYears(-3).ToString(MatchingCsvFileProcessor.AcceptedCsvDateFormats.First()) // Child age
            },
            new()
            {
                ["Id"] = "L3",
                ["Given"] = "Jim",
                ["Family"] = "Beam",
                ["DOB"] = DateTime.Now.AddYears(-1).ToString(MatchingCsvFileProcessor.AcceptedCsvDateFormats.First()) // Child age
            }
        };


        await processor.ProcessCsvFileAsync("test", headers, records, outputPath);

        // Assert
        var files = Directory.EnumerateFiles($"{tempDir}/Processed/Matched", "*.csv").ToList();
        _testFiles.AddRange(files);
        Assert.Single(_testFiles);

        var content = await File.ReadAllLinesAsync(_testFiles.First());

        Assert.Equal(3, content.Length); // Header + 1 record with Match

        var contentRowSplit = content[1].Split(",");
        Assert.DoesNotContain("123467890", contentRowSplit.Last());
    }

    public void Dispose()
    {
        // Cleanup any resources if necessary
        foreach (var file in _testFiles.Where(File.Exists))
        {
            File.Delete(file);
        }
        GC.SuppressFinalize(this);
    }
}