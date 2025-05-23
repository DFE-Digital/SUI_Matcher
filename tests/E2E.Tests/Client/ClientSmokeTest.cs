using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using SUI.Client.Core;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

namespace E2E.Tests.Client;

/**
 * Runs against the NHS PDS api, sensitive data is loaded from `.env` file
 */
public class ClientSmokeTest(AppHostRealFixture fixture, TempDirectoryFixture tempDirectoryFixture) : IClassFixture<AppHostRealFixture>, IClassFixture<TempDirectoryFixture>
{
    private readonly HttpClient _client = fixture.CreateHttpClient("yarp");

    [Fact(Skip="Only runs locally")]
    public async Task TestSmokeTestData()
    {
        await TestAsync("sui_batch_search_queries.csv", data =>
        {
            Assert.True(data.Records.Count > 1);
        });
    }

    private async Task TestAsync(string inputFileName, Action<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> assertions)
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
        File.Copy(Path.Combine("Resources", "Smoke", inputFileName), Path.Combine(appConfig.IncomingDirectory, inputFileName));
        monitor.Processed += (s, e) => tcs.SetResult();

        await tcs.Task; // wait for the file to be processed
        
        Assert.True(File.Exists(monitor.LastOperation!.AssertSuccess().OutputCsvFile));
        var data = CsvFile.GetData(monitor.LastOperation!.AssertSuccess().OutputCsvFile);

        assertions(data);

        cts.Cancel();
    }
}