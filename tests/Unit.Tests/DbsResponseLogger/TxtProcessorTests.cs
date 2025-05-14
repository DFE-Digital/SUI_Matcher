using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shared.Util;

using SUI.DBS.Response.Logger.Core;
using SUI.DBS.Response.Logger.Core.Extensions;
using SUI.DBS.Response.Logger.Core.Watcher;

using Unit.Tests.Util;

using ColumnMapping = System.Collections.Generic.Dictionary<int, string>;

namespace Unit.Tests.DbsResponseLogger;

[TestClass]
public class TxtProcessorTests
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
    public async Task TestDbsTxtResultsFile()
    {
        // ARRANGE
        var f = new Bogus.Faker("en_GB");
        var testData = new List<TestData>
        {
            new(new ColumnMapping()),

            new(new ColumnMapping
            {
                [(int) RecordColumn.Given] = f.Name.FirstName(),
                [(int) RecordColumn.Family] = f.Name.LastName(),
                [(int) RecordColumn.BirthDate] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [(int) RecordColumn.Gender] = (f.Random.Bool() ? 1 : 2).ToString(),
                [(int) RecordColumn.PostCode] = f.Address.ZipCode("??## #??"),
                [(int) RecordColumn.NhsNumber] = f.Random.Long().ToString(),
            }),

            new(new ColumnMapping()),

            new(new ColumnMapping
            {
                [(int) RecordColumn.Given] = f.Name.FirstName(),
                [(int) RecordColumn.Family] = f.Name.LastName(),
                [(int) RecordColumn.BirthDate] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [(int) RecordColumn.Gender] = (f.Random.Bool() ? 1 : 2).ToString(),
                [(int) RecordColumn.PostCode] = f.Address.ZipCode("??## #??"),
                [(int) RecordColumn.NhsNumber] = "",
            }),

            new(new ColumnMapping()),
        };

        // ACT
        var cts = new CancellationTokenSource();

        var logMessages = new List<string>();

        var provider = Bootstrap(logMessages);
        var monitor = provider.GetRequiredService<TxtFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = testData.Select(x => x.Record).ToList();

        await WriteTxtAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00002.txt"), data);

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

        Assert.AreEqual(1, logMessages.Count(x => x.Contains($"[MATCH_COMPLETED] MatchStatus: Match, AgeGroup: {GetAgeRange(testData[1])}, Gender: {GetGender(testData[1])}, Postcode: {GetPostCode(testData[1])}")));
        Assert.AreEqual(1, logMessages.Count(x => x.Contains("The DBS results file has 2 records, batch search resulted in Match='1' and NoMatch='1'")));
    }

    private static string GetPostCode(TestData recordData)
        => TxtFileProcessor.ToPostCode(recordData.Record[(int)RecordColumn.PostCode]);

    private static string GetGender(TestData recordData)
        => TxtFileProcessor.ToGender(recordData.Record[(int)RecordColumn.Gender]);

    private static string GetAgeRange(TestData recordData)
        => TxtFileProcessor.GetAgeGroup(ToDateOnly(recordData.Record[(int)RecordColumn.BirthDate])!.Value);

    private static DateOnly? ToDateOnly(string value)
        => DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;


    private ServiceProvider Bootstrap(List<string> logMessages)
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddLogging(b => b.AddDebug().AddProvider(new TestContextLoggerProvider(TestContext, logMessages)));

        servicesCollection.Configure<TxtWatcherConfig>(x =>
        {
            x.IncomingDirectory = _dir.IncomingDirectoryPath;
            x.ProcessedDirectory = _dir.ProcessedDirectoryPath;
        });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        servicesCollection.AddClientCore(config);

        return servicesCollection.BuildServiceProvider();
    }

    private class TestData
    {

        public string[] Record { get; set; }

        public TestData(ColumnMapping partial)
        {
            var stringArray = new string[61];

            foreach (var (key, val) in partial)
            {
                stringArray[key] = val;
            }
            Record = stringArray;
        }
    }

    private static async Task WriteTxtAsync(string fileName, List<string[]> records)
    {
        await using var writer = new StreamWriter(fileName);
        foreach (var record in records)
        {
            await writer.WriteLineAsync(string.Join(",", record.Select(item => $"\"{item}\"")));
        }
    }
}