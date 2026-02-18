using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shared.Models;

using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.Client.Core.Infrastructure.Http;

namespace E2E.Tests.Client;

public class E2EIntegrationTests(AppHostFixture fixture, TempDirectoryFixture tempDirectoryFixture) : IClassFixture<AppHostFixture>, IClassFixture<TempDirectoryFixture>
{
    private readonly HttpClient _client = fixture.CreateSecureClient();

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

        // Create monitoring directory and copy file in place
        var fileName = "single_match.csv";
        var inputFilePath = Path.Combine("Resources", "Csv", fileName); // Relative path
        var monitorDirectory = Directory.CreateTempSubdirectory();

        // Prepare output directory
        var relativeOutputDirectory = Path.GetRelativePath(
            Directory.GetCurrentDirectory(),
            Directory.CreateTempSubdirectory().FullName
        );

        // Create object graph for CsvFileMonitor
        var matchPersonApiService = new HttpApiMatchingService(_client);
        var mappingConfig = new CsvMappingConfig();
        var watcherConfig = Options.Create(new CsvWatcherConfig
        {
            IncomingDirectory = monitorDirectory.FullName,
            ProcessedDirectory = relativeOutputDirectory,
            EnableGenderSearch = true,
        });
        var fileProcessor = new MatchingCsvFileProcessor(
            NullLogger<MatchingCsvFileProcessor>.Instance,
            mappingConfig,
            matchPersonApiService,
            watcherConfig
        );
        var monitor = new CsvFileMonitor(watcherConfig, NullLogger<CsvFileMonitor>.Instance, fileProcessor);

        // Act

        // Start monitor and wait for file to be processed
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.Processed += (_, _) => tcs.SetResult();
        var cts = new CancellationTokenSource();
        _ = monitor.StartAsync(cts.Token);
        File.Copy(inputFilePath, Path.Combine(monitorDirectory.FullName, fileName));
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(500), cts.Token);
        await cts.CancelAsync();

        if (monitor.LastOperation?.Exception is not null)
        {
            throw monitor.LastOperation.Exception;
        }

        // Assert
        Assert.Contains(
            Directory.EnumerateDirectories(relativeOutputDirectory),
            d =>
            {
                return d.EndsWith("single_match")
                       && Directory.EnumerateFiles(d).Any(f => Path.GetFileName(f).StartsWith("stats_output__"));
            });
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

        var monitor = new CsvFileMonitor(Options.Create(appConfig), NullLogger<CsvFileMonitor>.Instance, fileProcessor);

        _ = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        File.Copy(Path.Combine("Resources", "Csv", inputFileName), Path.Combine(appConfig.IncomingDirectory, inputFileName));
        monitor.Processed += (_, _) => tcs.SetResult();

        await tcs.Task; // wait for the file to be processed

        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastOperation!.AssertSuccess().OutputCsvFile));
        var data = CsvFile.GetData(monitor.LastOperation!.AssertSuccess().OutputCsvFile);
        Assert.Single(data.Records);

        var row = data.Records[0];
        assertions(row);

        await cts.CancelAsync();
    }
}