using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shared.Models;

using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.Client.Core.Infrastructure.Http;

namespace E2E.Tests.Client;

public class ReconciliationTests(AppHostFixture fixture, TempDirectoryFixture tempDirectoryFixture) : IClassFixture<AppHostFixture>, IClassFixture<TempDirectoryFixture>
{
    private readonly HttpClient _client = fixture.CreateSecureClient();

    [Fact]
    public async Task TestOneRowCsvSingleLowConfidenceMatch()
    {
        await TestAsync("patient_low_confidence_match_9876543210.csv", x =>
        {
            var matchStatus = x[ReconciliationCsvFileProcessor.HeaderMatchStatus];
            var nhsNumber = x[ReconciliationCsvFileProcessor.HeaderNhsNo];
            Assert.Equal(nameof(MatchStatus.LowConfidenceMatch), matchStatus);
            Assert.False(string.IsNullOrWhiteSpace(nhsNumber));
            // Asser that DOB is marked as different
            var dobDiff = x[ReconciliationCsvFileProcessor.HeaderDifferences].Split(" ");
            Assert.Contains("BirthDate", dobDiff);
        }, stats =>
        {
            // Assert stats
            var count = stats[nameof(ReconciliationCsvProcessStats.Count)];
            Assert.Equal(1, count);
            Assert.Equal(1, stats[nameof(ReconciliationCsvProcessStats.MatchingStatusLowConfidenceMatch)]);
            Assert.Equal(1, stats[nameof(ReconciliationCsvProcessStats.DifferencesCount)]); // Only diff is DOB
        });
    }

    private async Task TestAsync(string inputFileName, Action<Dictionary<string, string>> assertions, Action<Dictionary<string, int>> statsAssertions)
    {
        var cts = new CancellationTokenSource();
        Path.GetTempPath();

        var matchPersonApiService = new HttpApiMatchingService(_client);
        var mappingConfig = new CsvMappingConfig();
        var logger = NullLogger<ReconciliationCsvFileProcessor>.Instance;
        var watcherConfig = Options.Create(new CsvWatcherConfig()
        {
            IncomingDirectory = tempDirectoryFixture.IncomingDirectoryPath,
            ProcessedDirectory = tempDirectoryFixture.ProcessedDirectoryPath
        });
        var fileProcessor = new ReconciliationCsvFileProcessor(logger, mappingConfig, matchPersonApiService, watcherConfig);

        var monitor = new CsvFileMonitor(watcherConfig, NullLogger<CsvFileMonitor>.Instance, fileProcessor);

        _ = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        File.Copy(Path.Combine("Resources", "Csv", inputFileName), Path.Combine(watcherConfig.Value.IncomingDirectory, inputFileName));
        monitor.Processed += (s, e) => tcs.SetResult();

        await tcs.Task; // wait for the file to be processed

        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastOperation!.AssertSuccess().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastOperation.AssertSuccess().StatsJsonFile));
        var data = CsvFile.GetData(monitor.LastOperation!.AssertSuccess().OutputCsvFile);

        var readStatsJson = await File.ReadAllTextAsync(monitor.LastOperation.AssertSuccess().StatsJsonFile, cts.Token);
        var statsData = JsonSerializer.Deserialize<Dictionary<string, int>>(readStatsJson);
        Assert.Single(data.Records);
        Assert.NotNull(statsData);


        var row = data.Records[0];
        assertions(row);
        statsAssertions(statsData);



        await cts.CancelAsync();
    }
}