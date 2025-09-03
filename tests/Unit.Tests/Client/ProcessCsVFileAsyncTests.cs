using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Models;

using SUI.Client.Core;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using SUI.Client.Core.Watcher;

namespace Unit.Tests.Client;

public class ProcessCsVFileAsyncTests : IDisposable
{
    private readonly List<string> _testFiles = [];

    [Fact]
    public async Task ProcessCsvFileAsync_PassesExistingAllOptionalFieldsToMatchPersonAsync()
    {
        var mockLogger = new Mock<ILogger<CsvFileProcessor>>();
        var mockApi = new Mock<IMatchPersonApiService>();
        var mapping = new CsvMappingConfig { /* set up mappings as needed */ };
        var watcherConfig = Options.Create(new CsvWatcherConfig { EnableGenderSearch = true });

        MatchPersonPayload? capturedPayload = null;
        mockApi.Setup(x => x.MatchPersonAsync(It.IsAny<MatchPersonPayload>()))
            .Callback<MatchPersonPayload>(p => capturedPayload = p)
            .ReturnsAsync(new PersonMatchResponse());

        var processor = new CsvFileProcessor(mockLogger.Object, mapping, mockApi.Object, watcherConfig);

        // Prepare test CSV file with all optional fields
        var tempDir = Path.GetTempPath();
        var filePath = Path.Combine(tempDir, "test.csv");
        var outputPath = tempDir;
        var headers = new HashSet<string>
        {
            "Given", "Family", "ActiveCIN", "ActiveCLA", "ActiveCP", "ActiveEHM", "Ethnicity", "ImmigrationStatus"
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
                ["ImmigrationStatus"] = "Settled"
            }
        };
        await CsvFileProcessor.WriteCsvAsync(filePath, headers, records);
        _testFiles.Add(filePath);

        await processor.ProcessCsvFileAsync(filePath, outputPath);

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
        var mockLogger = new Mock<ILogger<CsvFileProcessor>>();
        var mockApi = new Mock<IMatchPersonApiService>();
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

        var processor = new CsvFileProcessor(mockLogger.Object, mapping, mockApi.Object, watcherConfig);


        var filePath = Path.Combine(tempDir, "test.csv");
        var outputPath = tempDir;
        var headers = new HashSet<string>
        {
            "Id",
            "Given",
            "Family",
        };
        var records = new List<Dictionary<string, string>>
        {
            new()
            {
                ["Id"] = "L1",
                ["Given"] = "John",
                ["Family"] = "Smith"
            },
            new()
            {
                ["Id"] = "L2",
                ["Given"] = "Jane",
                ["Family"] = "Doe"
            },
            new()
            {
                ["Id"] = "L3",
                ["Given"] = "Jim",
                ["Family"] = "Beam"
            }
        };


        await CsvFileProcessor.WriteCsvAsync(filePath, headers, records);
        await processor.ProcessCsvFileAsync(filePath, outputPath);

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