using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Shared.Models;

using SUI.Client.Core;
using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;
using SUI.Core;
using SUI.Core.Endpoints;
using SUI.Core.Services;
using SUI.Test.Integration.Adapters;

using D = System.Collections.Generic.Dictionary<string, string>;

namespace SUI.Test.Integration;

[TestClass]
public class CsvProcessorTests
{
    private TempDirectoryFixture _dir = null!;

    public required TestContext TestContext { get; set; }

    [TestCleanup]
    public void Clean()
    {
        //_dir.Dispose();
    }

    [TestInitialize]
    public void Init()
    {
        _dir = new TempDirectoryFixture();
    }

    [TestMethod]
    public async Task TestCsvBatchFile()
    {
        // ARRANGE
        var f = new Bogus.Faker();
        var testData = new List<TestData>
        {
            new(new D
            {
                [CsvMappingConfig.Defaults.GivenName] = f.Name.FirstName(),
                [CsvMappingConfig.Defaults.Surname] = f.Name.LastName(),
                [CsvMappingConfig.Defaults.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [CsvMappingConfig.Defaults.Email] = f.Internet.Email(),
            }, SearchResult.Match("AAAAA1111111", 0.98m)),

            new(new D
            {
                [CsvMappingConfig.Defaults.GivenName] = f.Name.FirstName(),
                [CsvMappingConfig.Defaults.Surname] = f.Name.LastName(),
                [CsvMappingConfig.Defaults.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [CsvMappingConfig.Defaults.Email] = f.Internet.Email(),
            }, SearchResult.Match("AAAAA2222222", 0.95m)),

            new(new D
            {
                [CsvMappingConfig.Defaults.GivenName] = f.Name.FirstName(),
                [CsvMappingConfig.Defaults.Surname] = f.Name.LastName(),
                [CsvMappingConfig.Defaults.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [CsvMappingConfig.Defaults.Email] = f.Internet.Email(),
            }, SearchResult.Match("AAAAA333333", 0.87m)),

            new(new D
            {
                [CsvMappingConfig.Defaults.GivenName] = f.Name.FirstName(),
                [CsvMappingConfig.Defaults.Surname] = f.Name.LastName(),
                [CsvMappingConfig.Defaults.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [CsvMappingConfig.Defaults.Email] = f.Internet.Email(),
            }, SearchResult.MultiMatched()),

            new(new D
            {
                [CsvMappingConfig.Defaults.GivenName] = f.Name.FirstName(),
                [CsvMappingConfig.Defaults.Surname] = f.Name.LastName(),
                [CsvMappingConfig.Defaults.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [CsvMappingConfig.Defaults.Email] = f.Internet.Email(),
            }, SearchResult.Unmatched()),
        };

        var mockNhsApi = new Mock<INhsFhirClient>(MockBehavior.Loose);
        foreach (var testDataItem in testData)
        {
            mockNhsApi.Setup(x => x.PerformSearch(It.Is<SearchQuery>(y => y.Email == testDataItem.Data[CsvMappingConfig.Defaults.Email]))).Returns(() => Task.FromResult((SearchResult?)testDataItem.SearchResult));
        }

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(x =>
        {
            x.AddSingleton(mockNhsApi.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = testData.Select(x => x.Data).ToList();
        var headers = new HashSet<string>(data.First().Keys);

        await CsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00002.csv"), headers, data);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.IsNull(monitor.GetLastOperation().Exception, monitor.GetLastOperation().Exception?.ToString() ?? "Exception occurred");
        Assert.AreEqual(0, monitor.ErrorCount);
        Assert.AreEqual(1, monitor.ProcessedCount);
        Assert.IsTrue(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.IsTrue(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.IsTrue(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.IsNotNull(monitor.LastResult().Stats);

        var (_, records) = await CsvFileProcessor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.IsNotNull(records);
    }

    [TestMethod]
    public async Task TestOneFileSingleMatch()
    {
        var searchResult = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.98m, Type = SearchResult.ResultType.Matched };
        var mockNhsApi = new Mock<INhsFhirClient>(MockBehavior.Loose);
        mockNhsApi.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>())).Returns(() => Task.FromResult((SearchResult?)searchResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(x =>
        {
            x.AddSingleton(mockNhsApi.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [CsvMappingConfig.Defaults.GivenName] = "John",
            [CsvMappingConfig.Defaults.Surname] = "Smith",
            [CsvMappingConfig.Defaults.DOB] = "2000-04-01",
            [CsvMappingConfig.Defaults.Email] = "test@test.com",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.IsNull(monitor.GetLastOperation().Exception, monitor.GetLastOperation().Exception?.ToString() ?? "Exception occurred");
        Assert.AreEqual(0, monitor.ErrorCount);
        Assert.AreEqual(1, monitor.ProcessedCount);
        Assert.IsTrue(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.IsTrue(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.IsTrue(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.IsNotNull(monitor.LastResult().Stats);

        mockNhsApi.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.Once(), "The PerformSearch method should have invoked ONCE");
        var (_, records) = await CsvFileProcessor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.AreEqual(searchResult.NhsNumber, records.First()[CsvFileProcessor.HeaderNhsNo], "The NHS number doesn't match");
        Assert.AreEqual(searchResult.Score.ToString(), records.First()[CsvFileProcessor.HeaderScore], "The score doesn't match");
        Assert.AreEqual(nameof(MatchStatus.Match), records.First()[CsvFileProcessor.HeaderStatus], "The score doesn't match");
    }
    private ServiceProvider Bootstrap(Action<ServiceCollection>? configure = null)
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddLogging(b => b.AddDebug().AddProvider(new TestContextLoggerProvider(TestContext)));
        var config = new ConfigurationBuilder()
            .Build();

        servicesCollection.Configure<CsvWatcherConfig>(x =>
        {
            x.IncomingDirectory = _dir.IncomingDirectoryPath;
            x.ProcessedDirectory = _dir.ProcessedDirectoryPath;
        });

        servicesCollection.AddClientCore(config, "http://localhost");
        servicesCollection.AddSingleton<IMatchPersonApiService, MatchPersonServiceAdapter>(); // wires up the IMatchingService directly, without using http

        // core domain deps
        servicesCollection.AddSingleton<IMatchingService, MatchingService>();
        servicesCollection.AddSingleton<IValidationService, ValidationService>();

        configure?.Invoke(servicesCollection);
        return servicesCollection.BuildServiceProvider();
    }

    private class TestData(D data, SearchResult searchResult)
    {
        public D Data { get; set; } = data;
        public SearchResult SearchResult { get; set; } = searchResult;
    }
}