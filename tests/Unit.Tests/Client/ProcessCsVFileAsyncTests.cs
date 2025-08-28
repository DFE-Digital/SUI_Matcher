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
    public async Task ProcessCsvFileAsync_PassesOptionalFieldsToMatchPersonAsync()
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

        // Prepare test CSV file with optional fields
        var tempDir = Path.GetTempPath();
        var filePath = Path.Combine(tempDir, "test.csv");
        var outputPath = tempDir;
        var headers = new HashSet<string> { "Given", "Family", "ActiveCIN", "Ethnicity" };
        var records = new List<Dictionary<string, string>>
        {
            new() { ["Given"] = "Jane", ["Family"] = "Doe", ["ActiveCIN"] = "CIN123", ["Ethnicity"] = "A1 - White-British" }
        };
        await CsvFileProcessor.WriteCsvAsync(filePath, headers, records);

        await processor.ProcessCsvFileAsync(filePath, outputPath);

        Assert.NotNull(capturedPayload);
        Assert.True(capturedPayload!.OptionalFields.ContainsKey("ActiveCIN"));
        Assert.Equal("CIN123", capturedPayload.OptionalFields["ActiveCIN"]);
        Assert.True(capturedPayload.OptionalFields.ContainsKey("Ethnicity"));
        Assert.Equal("A1 - White-British", capturedPayload.OptionalFields["Ethnicity"]);

        // Clean up
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}