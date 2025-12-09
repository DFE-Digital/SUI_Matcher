using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shared.Models;

using SUI.Client.Core;
using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.Client.Core.Infrastructure.Http;

using WireMock.Client;

namespace E2E.Tests.Client;

public class E2EIntegrationTests(AppHostFixture fixture, TempDirectoryFixture tempDirectoryFixture) : IClassFixture<AppHostFixture>, IClassFixture<TempDirectoryFixture>
{
    private readonly HttpClient _client = fixture.CreateSecureClient();
    private readonly IWireMockAdminApi _nhsAuthMockApi = fixture.NhsAuthMockApi();

    [Fact]
    public async Task TestOneRowCsvSingleMatch()
    {
        await TestAsync("single_match.csv", x =>
        {
            var matchStatus = x[MatchingCsvFileProcessor.HeaderStatus];
            var nhsNumber = x[MatchingCsvFileProcessor.HeaderNhsNo];
            Assert.Equal(nameof(MatchStatus.Match), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }

    [Fact]
    public async Task TestOneRowDbsCsvSingleMatch()
    {
        await TestAsync("single_match_from_dbs_headers.csv", x =>
        {
            var matchStatus = x[MatchingCsvFileProcessor.HeaderStatus];
            var nhsNumber = x[MatchingCsvFileProcessor.HeaderNhsNo];
            Assert.Equal(nameof(MatchStatus.Match), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }

    [Fact]
    public async Task TestOneRowCsvSingleLowConfidence()
    {
        await TestAsync("single_match_low_confidence.csv", x =>
        {
            var matchStatus = x[MatchingCsvFileProcessor.HeaderStatus];
            var nhsNumber = x[MatchingCsvFileProcessor.HeaderNhsNo];
            Assert.Equal(nameof(MatchStatus.PotentialMatch), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }


    [Fact]
    public async Task TestOneRowCsvSingleReallyLowConfidence()
    {
        await TestAsync("single_match_really_low_confidence.csv", x =>
        {
            var matchStatus = x[MatchingCsvFileProcessor.HeaderStatus];
            var nhsNumber = x[MatchingCsvFileProcessor.HeaderNhsNo];
            Assert.Equal(nameof(MatchStatus.LowConfidenceMatch), matchStatus);
            Assert.Equal("9691292211", nhsNumber);
        });
    }

    [Fact]
    public async Task TestOneRowCsvSingleNoMatch()
    {
        await TestAsync("no_match.csv", x =>
                {
                    var matchStatus = x[MatchingCsvFileProcessor.HeaderStatus];
                    var nhsNumber = x[MatchingCsvFileProcessor.HeaderNhsNo];
                    Assert.Equal(nameof(MatchStatus.NoMatch), matchStatus);
                    Assert.Equal("-", nhsNumber);
                });

    }

    [Fact]
    public async Task ProcessCsvFileAsync_WritesToExpectedLocation_WhenUsingRelativePath()
    {
        // Arrange
        var matchPersonApiService = new HttpApiMatchingService(_client);
        var mappingConfig = new CsvMappingConfig();
        var logger = NullLogger<MatchingCsvFileProcessor>.Instance;
        // create IOptions<CsvWatcherConfig> if needed
        var watcherConfig = Options.Create(new CsvWatcherConfig());
        var fileProcessor = new MatchingCsvFileProcessor(logger, mappingConfig, matchPersonApiService, watcherConfig);

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

        var matchPersonApiService = new HttpApiMatchingService(_client);
        var mappingConfig = new CsvMappingConfig();
        var logger = NullLogger<MatchingCsvFileProcessor>.Instance;
        var watcherConfig = Options.Create(new CsvWatcherConfig());
        var fileProcessor = new MatchingCsvFileProcessor(logger, mappingConfig, matchPersonApiService, watcherConfig);

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