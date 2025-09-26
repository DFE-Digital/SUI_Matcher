using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shared.Util;

using SUI.DBS.Response.Logger.Core;
using SUI.DBS.Response.Logger.Core.Extensions;
using SUI.DBS.Response.Logger.Core.Watcher;

using Unit.Tests.Util;

using Xunit.Abstractions;

using ColumnMapping = System.Collections.Generic.Dictionary<int, string>;

namespace Unit.Tests.DbsResponseLogger;

public class TxtProcessorTests(ITestOutputHelper testOutputHelper)
{
    private readonly TempDirectoryFixture _dir = new();

    public required ITestOutputHelper TestContext = testOutputHelper;

    [Fact]
    public async Task TestDbsTxtResultsFile()
    {
        // ARRANGE
        using var activity = new Activity("TestActivity2");
        activity.Start();
        Activity.Current = activity;

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

        var data = testData.Select(x => x.Record).ToList();

        await WriteTxtAsync(_dir, "file0001.txt", data); // Today
        await WriteTxtAsync(_dir, "file0002.txt", data); // yesterday
        await WriteTxtAsync(_dir, "file0003.txt", data); // day before yesterday
        await WriteTxtAsync(_dir, "file0004.txt", data); // last year

        await WatchFile(monitor, data, () => Task.CompletedTask); // Today
        await WatchFile(monitor, data, () => UpdateFileModifiedDate(_dir, "file0002.txt", -1)); // yesterday
        await WatchFile(monitor, data, () => UpdateFileModifiedDate(_dir, "file0003.txt", -2)); // day before yesterday
        await WatchFile(monitor, data, () => UpdateFileModifiedDate(_dir, "file0004.txt", -365)); // last year

        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        string[] existingFiles = Directory.GetFiles(_dir.ProcessedDirectoryPath);
        string[] stillExistsFiles = Directory.GetFiles(_dir.IncomingDirectoryPath);
        Assert.Equal(4, stillExistsFiles.Length); // Proves we are not moving files, just processing them
        Assert.Equal(4, existingFiles.Length);

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(4, monitor.ProcessedCount);

        var jsonLogMessages = logMessages.Select(msg =>
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(msg);
                return dict?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );
            })
            .Where(dict => dict != null)
            .OfType<Dictionary<string, string>>()
            .ToList();

        Assert.Equal(4, jsonLogMessages.Count(x => x["Message"].Contains($"[MATCH_COMPLETED] MatchStatus: Match, AgeGroup: {GetAgeRange(testData[1])}, Gender: {GetGender(testData[1])}, Postcode: {GetPostCode(testData[1])}")));
        Assert.Equal(4, jsonLogMessages.Count(x => x["Message"].Contains("The DBS results file has 2 records, batch search resulted in Match='1' and NoMatch='1'")));

        AssertMatchLogWithTimeStampOfDayExists(2, jsonLogMessages, 0);
        AssertMatchLogWithTimeStampOfDayExists(2, jsonLogMessages, -1);
        AssertMatchLogWithTimeStampOfDayExists(2, jsonLogMessages, -2);
        AssertMatchLogWithTimeStampOfDayExists(2, jsonLogMessages, -365);
    }

    private static void AssertMatchLogWithTimeStampOfDayExists(int expectedCount, List<Dictionary<string, string>> jsonLogMessages, int relativeDay)
    {
        Assert.Equal(expectedCount, jsonLogMessages.Count(x =>
        {
            if (DateTime.TryParse(x["TimeStamp"], out DateTime timeStamp))
            {
                return timeStamp.Date == DateTime.Now.AddDays(relativeDay).Date &&
                       x["Message"].Contains("[MATCH_COMPLETED]");
            }

            return false;
        }));
    }

    private static async Task WatchFile(TxtFileMonitor monitor, List<string[]> data, Func<Task> action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await action.Invoke();
        EventHandler<FileProcessedEnvelope> handler = (_, _) => tcs.SetResult();
        monitor.Processed += handler;
        await tcs.Task; // await processing of that file
        monitor.Processed -= handler;
    }

    private static string GetPostCode(TestData recordData)
        => TxtFileProcessor.ToPostCode(recordData.Record[(int)RecordColumn.PostCode]);

    private static string GetGender(TestData recordData)
        => PersonSpecificationUtils.ToGenderFromNumber(recordData.Record[(int)RecordColumn.Gender]);

    private static string GetAgeRange(TestData recordData)
        => TxtFileProcessor.GetAgeGroup(ToDateOnly(recordData.Record[(int)RecordColumn.BirthDate])!.Value);

    private static DateOnly? ToDateOnly(string value)
        => DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;


    private ServiceProvider Bootstrap(List<string> logMessages)
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddLogging(b => b.AddDebug().AddProvider(new JsonTestContextLoggerProvider(TestContext, logMessages)));

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

    private static async Task WriteTxtAsync(TempDirectoryFixture dir, string fileName, List<string[]> records)
    {
        await using var writer = new StreamWriter(Path.Combine(dir.IncomingDirectoryPath, fileName));
        foreach (var record in records)
        {
            await writer.WriteLineAsync(string.Join(",", record.Select(item => $"\"{item}\"")));
        }
    }

    private static Task UpdateFileModifiedDate(TempDirectoryFixture dir, string fileName, int addDays)
    {
        var newModifiedDate = DateTime.Now.AddDays(addDays);
        File.SetLastWriteTime(Path.Combine(dir.IncomingDirectoryPath, fileName), newModifiedDate);

        return Task.CompletedTask;
    }
}