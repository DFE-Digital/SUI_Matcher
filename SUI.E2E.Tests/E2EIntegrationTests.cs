using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SUI.Client.Core;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;
using SUI.Core.Domain;
using SUI.E2E.Tests.Util;
using SUI.Types;
using WireMock.Client;
using Xunit.Abstractions;

namespace SUI.E2E.Tests;

public class E2EIntegrationTests(AppHostFixture fixture, TempDirectoryFixture tempDirectoryFixture, ITestOutputHelper testOutputHelper) : IClassFixture<AppHostFixture>, IClassFixture<TempDirectoryFixture>
{
    private readonly HttpClient _client = fixture.CreateHttpClient("yarp");
    private readonly IWireMockAdminApi _nhsAuthMockApi = fixture.NhsAuthMockApi();
    private readonly AppHostFixture _fixture = fixture;
    private readonly TempDirectoryFixture _tempDirectoryFixture = tempDirectoryFixture;

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


    private async Task TestAsync(string inputFileName, Action<Dictionary<string, string>> assertions)
    {
        var cts = new CancellationTokenSource();
        Path.GetTempPath();

        var matchPersonApiService = new MatchPersonApiService(_client);
        var mappingConfig = new CsvMappingConfig();
        var fileProcessor = new CsvFileProcessor(mappingConfig, matchPersonApiService);

        var appConfig = new CsvWatcherConfig()
        {
            IncomingDirectory = _tempDirectoryFixture.IncomingDirectoryPath,
            ProcessedDirectory = _tempDirectoryFixture.ProcessedDirectoryPath,
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

        cts.Cancel();

    }


}

