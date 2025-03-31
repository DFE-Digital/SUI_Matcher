using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using SUI.DBS.Client.Core;
using SUI.DBS.Client.Core.Extensions;
using SUI.DBS.Client.Core.Watcher;
using SUI.Core;
using SUI.Core.Endpoints;
using SUI.Core.Services;
using SUI.Test.Integration.Adapters;
using SUI.Types;
using D = System.Collections.Generic.Dictionary<int, string>;

namespace SUI.Test.Integration;

[TestClass]
public class TxtProcessorTests
{
    private TempDirectoryFixture _dir;

    public TestContext TestContext { get; set; }

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
            new(new D()),
            
            new(new D
            {
                [(int) RecordColumn.Given] = f.Name.FirstName(),
                [(int) RecordColumn.Family] = f.Name.LastName(),
                [(int) RecordColumn.BirthDate] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [(int) RecordColumn.Gender] = (f.Random.Bool() ? 1 : 2).ToString(),
                [(int) RecordColumn.PostCode] = f.Address.ZipCode("??## #??"),
                [(int) RecordColumn.NhsNumber] = f.Random.Long().ToString(),
            }),

            new(new D()),

            new(new D
            {
                [(int) RecordColumn.Given] = f.Name.FirstName(),
                [(int) RecordColumn.Family] = f.Name.LastName(),
                [(int) RecordColumn.BirthDate] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [(int) RecordColumn.Gender] = (f.Random.Bool() ? 1 : 2).ToString(),
                [(int) RecordColumn.PostCode] = f.Address.ZipCode("??## #??"),
                [(int) RecordColumn.NhsNumber] = "",
            }),

            new(new D()),
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

        monitor.Processed += (s, e) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        cts.Cancel();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.IsNull(monitor.GetLastOperation().Exception, monitor.GetLastOperation().Exception?.ToString() ?? "Exception occurred");
        Assert.AreEqual(0, monitor.ErrorCount);
        Assert.AreEqual(1, monitor.ProcessedCount);
        
        Assert.AreEqual(1, logMessages.Count(x => x.Contains($"The DBS search resulted in match status 'Match'")));
        Assert.AreEqual(1, logMessages.Count(x => x.Contains($"The DBS search resulted in match status 'NoMatch'")));
        Assert.AreEqual(1, logMessages.Count(x => x.Contains($"The DBS results file has 2 records, batch search resulted in Match='1' and NoMatch='1'")));
    }
    
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
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = $"InstrumentationKey={Guid.NewGuid()};IngestionEndpoint=http://localhost/;LiveEndpoint=http://localhost/;ApplicationId=${Guid.NewGuid()}"
            })
            .Build();
        servicesCollection.AddClientCore(config);
        
        return servicesCollection.BuildServiceProvider();
    }

    private class TestData
    {

        public string[] Record { get; set; }

        public TestData(D partial)
        {
            var stringArray = new string[61];

            foreach (var (key, val) in partial)
            {
                stringArray[key] = val;
            }
            Record = stringArray;
        }
    }
    
    public static async Task<string> WriteTxtAsync(string fileName, List<string[]> records)
    {
        await using var writer = new StreamWriter(fileName);
        foreach (var record in records)
        {
            await writer.WriteLineAsync(string.Join(",", record.Select(item => $"\"{item}\"")));
        }

        return fileName;
    }
}