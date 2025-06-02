using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shared.Models;

using SUI.Client.Core;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

using WireMock.Client;

namespace E2E.Tests.Client;

public class E2EIntegrationTests(AppHostFixture fixture, TempDirectoryFixture tempDirectoryFixture) : IClassFixture<AppHostFixture>, IClassFixture<TempDirectoryFixture>
{
    private readonly HttpClient _client = fixture.CreateHttpClient("yarp");
    private readonly IWireMockAdminApi _nhsAuthMockApi = fixture.NhsAuthMockApi();

    [Fact]
    public async Task TestOneRowCsvSingleMatch()
    {
        await TestAsync("single_match.csv", x =>
        {
            var matchStatus = x[CsvFileProcessor.HeaderStatus];
            var nhsNumber = x[CsvFileProcessor.HeaderNhsNo];
            Assert.Equal(MatchStatus.Match.ToString(), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }

    [Fact]
    public async Task TestOneRowCsvSingleLowConfidence()
    {
        await TestAsync("single_match_low_confidence.csv", x =>
        {
            var matchStatus = x[CsvFileProcessor.HeaderStatus];
            var nhsNumber = x[CsvFileProcessor.HeaderNhsNo];
            Assert.Equal(MatchStatus.PotentialMatch.ToString(), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }


    [Fact]
    public async Task TestOneRowCsvSingleReallyLowConfidence()
    {
        await TestAsync("single_match_really_low_confidence.csv", x =>
        {
            var matchStatus = x[CsvFileProcessor.HeaderStatus];
            var nhsNumber = x[CsvFileProcessor.HeaderNhsNo];
            Assert.Equal(MatchStatus.NoMatch.ToString(), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }

    [Fact]
    public async Task ProcessCsvFileAsync_WritesToExpectedLocation_WhenUsingRelativePath()
    {
        // Arrange
        var matchPersonApiService = new MatchPersonApiService(_client);
        var mappingConfig = new CsvMappingConfig();
        var fileProcessor = new CsvFileProcessor(mappingConfig, matchPersonApiService);

        var inputFileName = "single_match.csv";
        var inputFilePath = Path.Combine("Resources", "Csv", inputFileName); // Relative path

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var outputDirectory = Path.Combine("TestOutput"); // Use a relative path for output
        Directory.CreateDirectory(outputDirectory);

        var expectedOutputDirectory = Path.Combine(outputDirectory, $"_{timestamp}__single_match");
        var expectedOutputFilePath = Path.Combine(expectedOutputDirectory, "stats_output__" + timestamp + ".json");

        // Act
        await fileProcessor.ProcessCsvFileAsync(inputFilePath, outputDirectory);

        // Assert
        Assert.True(Directory.Exists(expectedOutputDirectory), "Output directory was not created.");
        Assert.True(File.Exists(expectedOutputFilePath), "Output file was not created in the expected location.");

        // Cleanup
        if (Directory.Exists(expectedOutputDirectory))
        {
            Directory.Delete(expectedOutputDirectory, true);
        }
    }


    private async Task TestAsync(string inputFileName, Action<Dictionary<string, string>> assertions)
    {
        var cts = new CancellationTokenSource();
        Path.GetTempPath();

        var matchPersonApiService = new MatchPersonApiService(_client);
        var mappingConfig = new CsvMappingConfig();
        var fileProcessor = new CsvFileProcessor(mappingConfig, matchPersonApiService);

        var appConfig = new CsvWatcherConfig()
        {
            IncomingDirectory = tempDirectoryFixture.IncomingDirectoryPath,
            ProcessedDirectory = tempDirectoryFixture.ProcessedDirectoryPath,
        };

        var watcher = new CsvFileWatcherService(Options.Create(appConfig), NullLoggerFactory.Instance);
        var monitor = new CsvFileMonitor(watcher, Options.Create(appConfig), NullLogger<CsvFileMonitor>.Instance, fileProcessor);

        var monitoringTask = monitor.StartAsync(cts.Token);

        Assert.Equal(0, watcher.Count);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        File.Copy(Path.Combine("Resources", "Csv", inputFileName), Path.Combine(appConfig.IncomingDirectory, inputFileName));
        monitor.Processed += (s, e) => tcs.SetResult();

        await tcs.Task; // wait for the file to be processed

        Assert.Equal(1, watcher.Count);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastOperation!.AssertSuccess().OutputCsvFile));
        var data = CsvFile.GetData(monitor.LastOperation!.AssertSuccess().OutputCsvFile);
        Assert.Single(data.Records);

        var row = data.Records[0];
        assertions(row);

        await cts.CancelAsync();

    }



}