using System.Threading.Channels;

using MatchingApi.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

using Moq;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;
using Shared.Util;

using SUI.Client.Core;
using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

using Unit.Tests.Util;
using Unit.Tests.Util.Adapters;

using Xunit.Abstractions;

using D = System.Collections.Generic.Dictionary<string, string>;

namespace Unit.Tests.Client;

public class CsvProcessorTests(ITestOutputHelper testOutputHelper)
{
    private readonly TempDirectoryFixture _dir = new();
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<IMatchingService> _matchingService = new(MockBehavior.Loose);

    public required ITestOutputHelper TestContext = testOutputHelper;

    private static class TestDataHeaders
    {
        public const string GivenName = "GivenName";
        public const string Surname = "Surname";
        public const string DOB = "DOB";
        public const string Email = "Email";
        public const string Gender = "Gender";
        public const string NhsNumber = "NhsNumber";
        public const string PostCode = "PostCode";
        public static string Phone = "Phone";
    }

    [Fact]
    public async Task TestCsvBatchFile()
    {
        // ARRANGE
        var f = new Bogus.Faker();
        var testData = new List<TestData>
        {
            new(new D
            {
                [TestDataHeaders.GivenName] = f.Name.FirstName(),
                [TestDataHeaders.Surname] = f.Name.LastName(),
                [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [TestDataHeaders.Email] = f.Internet.Email(),
            }, SearchResult.Match("AAAAA1111111", 0.98m)),

            new(new D
            {
                [TestDataHeaders.GivenName] = f.Name.FirstName(),
                [TestDataHeaders.Surname] = f.Name.LastName(),
                [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [TestDataHeaders.Email] = f.Internet.Email(),
            }, SearchResult.Match("AAAAA2222222", 0.95m)),

            new(new D
            {
                [TestDataHeaders.GivenName] = f.Name.FirstName(),
                [TestDataHeaders.Surname] = f.Name.LastName(),
                [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [TestDataHeaders.Email] = f.Internet.Email(),
            }, SearchResult.Match("AAAAA333333", 0.87m)),

            new(new D
            {
                [TestDataHeaders.GivenName] = f.Name.FirstName(),
                [TestDataHeaders.Surname] = f.Name.LastName(),
                [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [TestDataHeaders.Email] = f.Internet.Email(),
            }, SearchResult.MultiMatched()),

            new(new D
            {
                [TestDataHeaders.GivenName] = f.Name.FirstName(),
                [TestDataHeaders.Surname] = f.Name.LastName(),
                [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
                [TestDataHeaders.Email] = f.Internet.Email(),
            }, SearchResult.Unmatched()),
        };

        foreach (var testDataItem in testData)
        {
            _nhsFhirClient.Setup(x => x.PerformSearch(It.Is<SearchQuery>(y => y.Email == testDataItem.Data[TestDataHeaders.Email]))).Returns(() => Task.FromResult<SearchResult?>(testDataItem.SearchResult));
        }

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = testData.Select(x => x.Data).ToList();
        var headers = new HashSet<string>(data.First().Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00002.csv"), headers, data);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        var (_, records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.NotNull(records);
    }

    [Fact]
    public async Task TestOneFileSingleMatch()
    {
        var searchResult = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.98m, Type = SearchResult.ResultType.Matched };
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>())).Returns(() => Task.FromResult<SearchResult?>(searchResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Email] = "test@test.com",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.Once(), "The PerformSearch method should have invoked ONCE");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.Equal(searchResult.NhsNumber, records.First()[MatchingCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(searchResult.Score.ToString(), records.First()[MatchingCsvFileProcessor.HeaderScore]);
        Assert.Equal(nameof(MatchStatus.Match), records.First()[MatchingCsvFileProcessor.HeaderStatus]);
    }

    [Fact]
    public async Task CsvFile_ShouldContainLowConfidenceMatch()
    {
        var searchResult = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.849m, Type = SearchResult.ResultType.Matched };
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>())).Returns(() => Task.FromResult<SearchResult?>(searchResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Email] = "test@test.com",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00005.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.AtLeast(1), "The PerformSearch method should be called multiple times");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.Equal(searchResult.NhsNumber, records.First()[MatchingCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(searchResult.Score.ToString(), records.First()[MatchingCsvFileProcessor.HeaderScore]);
        Assert.Equal(nameof(MatchStatus.LowConfidenceMatch), records.First()[MatchingCsvFileProcessor.HeaderStatus]);
    }

    [Fact]
    public async Task StatsFile_ShouldContainsLowConfidenceStats()
    {
        var searchResult = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.849m, Type = SearchResult.ResultType.Matched };
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>())).Returns(() => Task.FromResult<SearchResult?>(searchResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Email] = "test@test.com",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00005.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.AtLeast(1), "The PerformSearch method should be called multiple times");
        var stats = await CsvFileProcessorBase.ReadStatsJsonFileAsync(monitor.GetLastOperation().AssertSuccess()
            .StatsJsonFile);

        Assert.True(stats.ContainsKey("CountLowConfidenceMatch"), "Stats file should contains LowConfidenceMatches key");
        Assert.True(stats.ContainsKey("LowConfidenceMatchPercentage"), "Stats file should contains LowConfidenceMatches key");
        Assert.Equal(1, stats.GetValueOrDefault("CountLowConfidenceMatch"));
        Assert.Equal(100, stats.GetValueOrDefault("LowConfidenceMatchPercentage"));

        // Cleanup files


    }

    [Fact]
    public async Task TestOneFileSingleMatch_GenderIsConverted()
    {
        var searchResult = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.98m, Type = SearchResult.ResultType.Matched };
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>())).Returns(() => Task.FromResult<SearchResult?>(searchResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });

        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith-G",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Gender] = "1",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);
        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00004.csv"), headers, list);


        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await file watcher stop

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.Equal("male", records.First()["Gender"]);

    }

    [Fact]
    public async Task TestOneFileSingleMatch_GenderNotSentIfGenderFlagIsOff()
    {
        var searchResultBad = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.55m, Type = SearchResult.ResultType.Unmatched };
        var searchResultGood = new SearchResult { NhsNumber = "AAAAA1111111", Score = 0.99m, Type = SearchResult.ResultType.Matched };

        // Mimick at least 3 calls to the PerformSearch method, showing different stages.
        _nhsFhirClient.SetupSequence(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .Returns(() => Task.FromResult<SearchResult?>(searchResultBad))
            .Returns(() => Task.FromResult<SearchResult?>(searchResultBad))
            .Returns(() => Task.FromResult<SearchResult?>(searchResultGood));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
            x.Configure<CsvWatcherConfig>(wc =>
            {
                wc.IncomingDirectory = _dir.IncomingDirectoryPath;
                wc.ProcessedDirectory = _dir.ProcessedDirectoryPath;
                wc.EnableGenderSearch = false; // Disable
            });
        });

        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith-G",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Gender] = "1",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);
        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00004.csv"), headers, list);


        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await file watcher stop

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        // Will retry after not getting the first match
        _nhsFhirClient.Verify(x => x.PerformSearch(It.Is<SearchQuery>(sq => sq.Gender == null)), Times.AtLeast(3), "The PerformSearch method should have invoked ONCE");
    }

    [Fact]
    public async Task FileProcessor_ThrowsException_IfFileStaysLocked()
    {
        // ARRANGE
        var f = new Bogus.Faker();
        var testData = new TestData(new D
        {
            [TestDataHeaders.GivenName] = f.Name.FirstName(),
            [TestDataHeaders.Surname] = f.Name.LastName(),
            [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
            [TestDataHeaders.Email] = f.Internet.Email(),
        }, SearchResult.Match("AAAAA1111111", 0.98m));

        _nhsFhirClient.Setup(x => x.PerformSearch(It.Is<SearchQuery>(y => y.Email == testData.Data[TestDataHeaders.Email]))).Returns(() => Task.FromResult<SearchResult?>(testData.SearchResult));

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new HashSet<string>(testData.Data.Keys);
        string filePath = Path.Combine(_dir.IncomingDirectoryPath, "file00003.csv");
        await CsvFileProcessorBase.WriteCsvAsync(filePath, headers, new List<D> { testData.Data });

        monitor.Processed += (_, _) => tcs.SetResult();

        // Simulate file being locked by another process
        await using (File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            await tcs.Task; // await processing of that file
            await cts.CancelAsync();   // cancel the task
            await monitoringTask; // await cancellation
        }

        // Assert exception is thrown and retries are attempted
        Assert.NotNull(monitor.GetLastOperation().Exception);
        Assert.IsType<IOException>(monitor.GetLastOperation().Exception);
    }

    [Fact]
    public async Task FileProcessor_Processes_AfterFileIsUnlocked()
    {
        // ARRANGE
        var f = new Bogus.Faker();
        var testData = new TestData(new D
        {
            [TestDataHeaders.GivenName] = f.Name.FirstName(),
            [TestDataHeaders.Surname] = f.Name.LastName(),
            [TestDataHeaders.DOB] = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1)).ToString(Constants.DateFormat),
            [TestDataHeaders.Email] = f.Internet.Email(),
        }, SearchResult.Match("AAAAA1111111", 0.98m));

        _nhsFhirClient.Setup(x => x.PerformSearch(It.Is<SearchQuery>(y => y.Email == testData.Data[TestDataHeaders.Email]))).Returns(() => Task.FromResult<SearchResult?>(testData.SearchResult));

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new HashSet<string>(testData.Data.Keys);
        string filePath = Path.Combine(_dir.IncomingDirectoryPath, "file00003.csv");
        await CsvFileProcessorBase.WriteCsvAsync(filePath, headers, [testData.Data]);

        monitor.Processed += (_, _) => tcs.SetResult();

        // Simulate file being locked by another process
        await using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            await Task.Delay(2000, cts.Token); // wait for retry logic to kick in
            stream.Close();
            await tcs.Task; // await processing of that file
            await cts.CancelAsync();   // cancel the task
            await monitoringTask;

        }

        // Assert
        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
    }

    [Fact]
    public async Task Reconciliation_ContainsAllHeaders()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
                AddressHistory = ["home~64 Higher Street~Leeds~West Yorkshire~LS123EA|", "billing~54 Medium,Street~Leeds~West Yorkshire~LS123EH|"],
                GeneralPractitionerOdsId = "Y12345"
            }
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false)).ReturnsAsync(
            new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = demographicResult.Result.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "9449305552",
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        var addressHistoryFormatted = $"\"{string.Join(" ", demographicResult.Result.AddressHistory)}\"";

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(demographicResult.Result.NhsNumber, records[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderGivenName], demographicResult.Result.GivenNames);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderFamilyName], demographicResult.Result.FamilyNames);
        Assert.Equal(demographicResult.Result.BirthDate.ToString(), records[0][ReconciliationCsvFileProcessor.HeaderBirthDate]);
        Assert.Equal(demographicResult.Result.Gender, records[0][ReconciliationCsvFileProcessor.HeaderGender]);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderAddressPostalCode], demographicResult.Result.AddressPostalCodes);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderEmail], demographicResult.Result.Emails);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderPhone], demographicResult.Result.PhoneNumbers);
        Assert.Equal(nameof(ReconciliationStatus.NoDifferences), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Contains(String.Empty, records[0][ReconciliationCsvFileProcessor.HeaderDifferences]);
        Assert.Equal(records[0][ReconciliationCsvFileProcessor.HeaderAddressHistory], addressHistoryFormatted);
        Assert.Equal(records[0][ReconciliationCsvFileProcessor.HeaderGeneralPractitionerOdsId], demographicResult.Result.GeneralPractitionerOdsId);
    }

    [Fact]
    public async Task Reconciliation_NoDifferences()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"]
            }
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false)).ReturnsAsync(
            new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = demographicResult.Result.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "9449305552",
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567"
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(demographicResult.Result.NhsNumber, records[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderGivenName], demographicResult.Result.GivenNames);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderFamilyName], demographicResult.Result.FamilyNames);
        Assert.Equal(demographicResult.Result.BirthDate.ToString(), records[0][ReconciliationCsvFileProcessor.HeaderBirthDate]);
        Assert.Equal(demographicResult.Result.Gender, records[0][ReconciliationCsvFileProcessor.HeaderGender]);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderAddressPostalCode], demographicResult.Result.AddressPostalCodes);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderEmail], demographicResult.Result.Emails);
        Assert.Contains(records[0][ReconciliationCsvFileProcessor.HeaderPhone], demographicResult.Result.PhoneNumbers);
        Assert.Equal(nameof(ReconciliationStatus.NoDifferences), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Contains(String.Empty, records[0][ReconciliationCsvFileProcessor.HeaderDifferences]);
    }

    [Fact]
    public async Task Reconciliation_OneDifference()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
            }
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false)).ReturnsAsync(
            new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = demographicResult.Result.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "9449305552",
            [TestDataHeaders.GivenName] = "Dave",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567"
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have been invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(nameof(ReconciliationStatus.Differences), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.DoesNotContain(records[0][TestDataHeaders.GivenName], demographicResult.Result.GivenNames);
        Assert.Equal("Given", records[0][ReconciliationCsvFileProcessor.HeaderDifferences]);
    }

    [Fact]
    public async Task Reconciliation_ManyDifferences()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
            }
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false)).ReturnsAsync(
            new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.ManyMatch,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "9449305552",
            [TestDataHeaders.GivenName] = "Dave",
            [TestDataHeaders.Surname] = "Wilkes",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567"
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have been invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.DoesNotContain(records[0][TestDataHeaders.Surname], demographicResult.Result.FamilyNames);
        Assert.Equal(nameof(ReconciliationStatus.Differences), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Equal("Given - Family - MatchingNhsNumber:NHS", records[0][ReconciliationCsvFileProcessor.HeaderDifferences]);
    }

    [Fact]
    public async Task Reconciliation_SupersededNhsNumber()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
            }
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false)).ReturnsAsync(
            new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.PotentialMatch,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "9691914913",
            [TestDataHeaders.GivenName] = "Dave",
            [TestDataHeaders.Surname] = "Wilkes",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567"
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once(), "The PerformSearchByNhsId method should have been invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(demographicResult.Result.NhsNumber, records[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(nameof(ReconciliationStatus.SupersededNhsNumber), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Contains("NhsNumber - Given - Family", records[0][ReconciliationCsvFileProcessor.HeaderDifferences]);
    }

    [Fact]
    public async Task Reconciliation_MissingNhsNumber()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
            }
        };

        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "",
            [TestDataHeaders.GivenName] = "Dave",
            [TestDataHeaders.Surname] = "Wilkes",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567"
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Never(), "The PerformSearchByNhsId method should have been invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal("-", records[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(nameof(ReconciliationStatus.MissingNhsNumber), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
    }

    [Fact]
    public async Task Reconciliation_InvalidNhsNumber()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smith"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
            }
        };

        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(It.IsAny<string>())).Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "99999999",
            [TestDataHeaders.GivenName] = "Dave",
            [TestDataHeaders.Surname] = "Wilkes",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.Gender] = "1",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567"
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await CsvFileProcessorBase.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), headers, list);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.True(File.Exists(monitor.LastResult().ReportPdfFile));
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Never(), "The PerformSearchByNhsId method should have been invoked once");
        (_, List<D> records) = await CsvFileProcessorBase.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(nameof(ReconciliationStatus.InvalidNhsNumber), records[0][ReconciliationCsvFileProcessor.HeaderStatus]);
    }

    private ServiceProvider Bootstrap(bool enableReconciliation, Action<ServiceCollection>? configure = null)
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddLogging(b => b.AddDebug().AddProvider(new TestContextLoggerProvider(TestContext)));
        var config = new ConfigurationBuilder()
            .Build();

        servicesCollection.Configure<CsvWatcherConfig>(x =>
        {
            x.IncomingDirectory = _dir.IncomingDirectoryPath;
            x.ProcessedDirectory = _dir.ProcessedDirectoryPath;
            x.EnableGenderSearch = true;
        });

        servicesCollection.AddClientCore(config, enableReconciliation);
        servicesCollection.AddSingleton<IMatchPersonApiService, MatchPersonServiceAdapter>(); // wires up the IMatchingService directly, without using http

        // core domain deps
        servicesCollection.AddSingleton<IMatchingService, MatchingService>();
        servicesCollection.AddSingleton<IReconciliationService, ReconciliationService>();
        servicesCollection.AddSingleton<IValidationService, ValidationService>();
        servicesCollection.AddSingleton<IAuditLogger, ChannelAuditLogger>();
        servicesCollection.AddSingleton(Channel.CreateUnbounded<AuditLogEntry>());
        servicesCollection.AddFeatureManagement();
        servicesCollection.AddSingleton<IConfiguration>(config);

        configure?.Invoke(servicesCollection);
        return servicesCollection.BuildServiceProvider();
    }

    private class TestData(D data, SearchResult searchResult)
    {
        public D Data { get; set; } = data;
        public SearchResult SearchResult { get; set; } = searchResult;
    }
}