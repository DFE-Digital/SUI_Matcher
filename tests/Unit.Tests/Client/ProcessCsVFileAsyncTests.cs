using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Models;

using SUI.Client.Core;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using SUI.Client.Core.Watcher;

namespace Unit.Tests.Client;

public class ProcessCsVFileAsyncTests
{
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

        await processor.ProcessCsvFileAsync(filePath, outputPath);

        Assert.NotNull(capturedPayload);
        Assert.Equal("CIN123", capturedPayload!.OptionalProperties["ActiveCIN"]);
        Assert.Equal("CLA456", capturedPayload.OptionalProperties["ActiveCLA"]);
        Assert.Equal("CP789", capturedPayload.OptionalProperties["ActiveCP"]);
        Assert.Equal("EHM321", capturedPayload.OptionalProperties["ActiveEHM"]);
        Assert.Equal("A1 - White-British", capturedPayload.OptionalProperties["Ethnicity"]);
        Assert.Equal("Settled", capturedPayload.OptionalProperties["ImmigrationStatus"]);

        // Clean up
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}