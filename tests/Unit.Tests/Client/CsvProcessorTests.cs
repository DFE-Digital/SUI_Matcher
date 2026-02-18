using System.Data;
using System.Threading.Channels;

using MatchingApi.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

using Moq;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;
using Shared.Util;

using SUI.Client.Core;
using SUI.Client.Core.Infrastructure.FileSystem;

using Unit.Tests.Util;
using Unit.Tests.Util.Adapters;

using Xunit.Abstractions;

using D = System.Collections.Generic.Dictionary<string, string>;
using IMatchingService = SUI.Client.Core.Application.Interfaces.IMatchingService;

namespace Unit.Tests.Client;

public class CsvProcessorTests(ITestOutputHelper testOutputHelper)
{
    private readonly TempDirectoryFixture _dir = new();
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<Shared.Endpoint.IMatchingService> _matchingService = new(MockBehavior.Loose);

    public required ITestOutputHelper TestContext = testOutputHelper;

    private class TestData(D data, SearchResult searchResult)
    {
        public D Data { get; set; } = data;
        public SearchResult SearchResult { get; set; } = searchResult;
    }

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

        var inputData = new DataTable("file00002");
        foreach (string key in testData[0].Data.Keys)
        {
            inputData.Columns.Add(key);
        }
        foreach (var testDataItem in testData)
        {
            inputData.Rows.Add(testDataItem.Data.Values);
        }

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.NotEmpty(outputData.Rows);
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

        var inputData = new DataTable("file00001")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email },
            Rows = { { "John", "Smith", "2000-04-01", "test@test.com" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(
            x => x.PerformSearch(It.IsAny<SearchQuery>()),
            Times.Once(),
            "The PerformSearch method should have invoked ONCE");

        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.Equal(searchResult.NhsNumber, outputData.Rows[0][MatchingCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(searchResult.Score.ToString(), outputData.Rows[0][MatchingCsvFileProcessor.HeaderScore]);
        Assert.Equal(nameof(MatchStatus.Match), outputData.Rows[0][MatchingCsvFileProcessor.HeaderStatus]);
    }

    [Fact]
    public async Task CsvFile_ShouldContainLowConfidenceMatch()
    {
        var searchResult = new SearchResult
        {
            NhsNumber = "AAAAA1111111",
            Score = 0.849m,
            Type = SearchResult.ResultType.Matched
        };
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .Returns(() => Task.FromResult<SearchResult?>(searchResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);


        var inputData = new DataTable("file00001")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email },
            Rows = { { "John", "Smith", "2000-04-01", "test@test.com" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00005.csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.AtLeast(1), "The PerformSearch method should be called multiple times");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.Equal(searchResult.NhsNumber, outputData.Rows[0][MatchingCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(searchResult.Score.ToString(), outputData.Rows[0][MatchingCsvFileProcessor.HeaderScore]);
        Assert.Equal(nameof(MatchStatus.LowConfidenceMatch), outputData.Rows[0][MatchingCsvFileProcessor.HeaderStatus]);
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

        var inputData = new DataTable("file00005")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email },
            Rows = { { "John", "Smith", "2000-04-01", "test@test.com" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.AtLeast(1), "The PerformSearch method should be called multiple times");
        var stats = await ReconciliationCsvFileProcessor.ReadStatsJsonFileAsync(monitor.GetLastOperation().AssertSuccess()
            .StatsJsonFile);

        Assert.True(stats.ContainsKey("CountLowConfidenceMatch"), "Stats file should contains LowConfidenceMatches key");
        Assert.True(stats.ContainsKey("LowConfidenceMatchPercentage"), "Stats file should contains LowConfidenceMatches key");
        Assert.Equal(1, stats.GetValueOrDefault("CountLowConfidenceMatch"));
        Assert.Equal(100, stats.GetValueOrDefault("LowConfidenceMatchPercentage"));

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

        var inputData = new DataTable("file00004")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email, TestDataHeaders.Gender },
            Rows = { { "John", "Smith-G", "2000-04-01", "test@test.com", "1" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);
        Assert.Equal("male", outputData.Rows[0]["Gender"]);

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

        var inputData = new DataTable("file00004")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email, TestDataHeaders.Gender },
            Rows = { { "John", "Smith-G", "2000-04-01", "test@test.com", "1" } }
        };
        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);


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
        _nhsFhirClient.Setup(x => x
            .PerformSearch(It.IsAny<SearchQuery>()))
            .Returns(() => Task.FromResult<SearchResult?>(SearchResult.Match("AAAAA1111111", 0.98m)));

        var f = new Bogus.Faker();
        var dob = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1))
            .ToString(Constants.DateFormat);
        var inputData = new DataTable("file00003")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email },
            Rows = { { f.Name.FirstName(), f.Name.LastName(), dob, f.Internet.Email() } },
        };

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        string filePath = Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv");
        await ReconciliationCsvFileProcessor.WriteCsvAsync(filePath, inputData);

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
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .Returns(() => Task.FromResult<SearchResult?>(SearchResult.Match("AAAAA1111111", 0.98m)));

        var f = new Bogus.Faker();
        var dob = f.Date.BetweenDateOnly(new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1))
            .ToString(Constants.DateFormat);
        var inputData = new DataTable("file00003")
        {
            Columns = { TestDataHeaders.GivenName, TestDataHeaders.Surname, TestDataHeaders.DOB, TestDataHeaders.Email },
            Rows = { { f.Name.FirstName(), f.Name.LastName(), dob, f.Internet.Email() } },
        };

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(false, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        string filePath = Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv");
        await ReconciliationCsvFileProcessor.WriteCsvAsync(filePath, inputData);

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
                AddressHistory = ["home~64 Higher Street~Leeds~West Yorkshire~LS123EA|", "billing~54 Medium Street~Leeds~West Yorkshire~LS123EH|"],
                GeneralPractitionerOdsId = "Y12345"
            }
        };
        var personMatchResponse = new PersonMatchResponse
        {
            Result = new MatchResult
            {
                MatchStatus = MatchStatus.Match,
                NhsNumber = demographicResult.Result.NhsNumber,
                Score = 1,
                ProcessStage = "ExactAll",
            }
        };
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(personMatchResponse);
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(It.IsAny<string>()))
            .Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = {{
                "9449305552",
                "John",
                "Smith",
                "2000-04-01",
                "1",
                "ab12 3ed",
                "test@test.com",
                "0789 1234567"
            }}
        };

        var addressHistoryFormatted = CsvUtils.WrapInputForCsv(demographicResult.Result.AddressHistory);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(demographicResult.Result.NhsNumber, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderGivenName], demographicResult.Result.GivenNames);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderFamilyName], demographicResult.Result.FamilyNames);
        Assert.Equal(demographicResult.Result.BirthDate.ToString(), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderBirthDate]);
        Assert.Equal(demographicResult.Result.Gender, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderGender]);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderAddressPostalCode], demographicResult.Result.AddressPostalCodes);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderEmail], demographicResult.Result.Emails);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderPhone], demographicResult.Result.PhoneNumbers);
        Assert.Equal(nameof(ReconciliationStatus.NoDifferences), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Contains(String.Empty, outputData.Rows[0].Field<string>(ReconciliationCsvFileProcessor.HeaderDifferences));
        Assert.Equal(addressHistoryFormatted, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderAddressHistory]);
        Assert.Equal(demographicResult.Result.GeneralPractitionerOdsId, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderGeneralPractitionerOdsId]);
        Assert.Equal(personMatchResponse.Result.Score.ToString(), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderMatchScore]);
        Assert.Equal(personMatchResponse.Result.ProcessStage, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderMatchProcessStage]);
    }
    [Fact]
    public async Task Reconciliation_ContainsAllHeaders_With_SpecialCharacters_in_Address_History()
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

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
            },
            Rows = { { "9449305552", "John", "Smith-G", "2000-04-01", "1", "ab12 3ed" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync();   // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        var addressHistoryFormatted = CsvUtils.WrapInputForCsv(demographicResult.Result.AddressHistory);
        Assert.Equal(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderAddressHistory], addressHistoryFormatted);

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

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = { { "9449305552", "John", "Smith", "2000-04-01", "1", "ab12 3ed", "test@test.com", "0789 1234567" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(demographicResult.Result.NhsNumber, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderGivenName], demographicResult.Result.GivenNames);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderFamilyName], demographicResult.Result.FamilyNames);
        Assert.Equal(demographicResult.Result.BirthDate.ToString(), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderBirthDate]);
        Assert.Equal(demographicResult.Result.Gender, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderGender]);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderAddressPostalCode], demographicResult.Result.AddressPostalCodes);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderEmail], demographicResult.Result.Emails);
        Assert.Contains(outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderPhone], demographicResult.Result.PhoneNumbers);
        Assert.Equal(nameof(ReconciliationStatus.NoDifferences), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Contains(String.Empty, outputData.Rows[0].Field<string>(ReconciliationCsvFileProcessor.HeaderDifferences));
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

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = { { "9449305552", "Dave", "Smith", "2000-04-01", "1", "ab12 3ed", "test@test.com", "0789 1234567" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have been invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(nameof(ReconciliationStatus.Differences), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.DoesNotContain(outputData.Rows[0][TestDataHeaders.GivenName], demographicResult.Result.GivenNames);
        Assert.Equal("Given", outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderDifferences]);
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
                    NhsNumber = "9999999993",
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

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = { { "9691914913", "Dave", "Wilkes", "2000-04-01", "1", "ab12 3ed", "test@test.com", "0789 1234567" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.AtLeastOnce, "The PerformSearchByNhsId method should have been invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(demographicResult.Result.NhsNumber, outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(nameof(ReconciliationStatus.LocalNhsNumberIsSuperseded), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderStatus]);
        Assert.Contains("NhsNumber - Given - Family", outputData.Rows[0].Field<string>(ReconciliationCsvFileProcessor.HeaderDifferences));
    }

    [Fact]
    public async Task Reconciliation_MissingNhsNumber()
    {
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = { { "", "Dave", "Wilkes", "2000-04-01", "1", "ab12 3ed", "test@test.com", "0789 1234567" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, "file00001.csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Never(), "The PerformSearchByNhsId method should have been invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal("-", outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderNhsNo]);
        Assert.Equal(nameof(ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderStatus]);
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
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false)).ReturnsAsync(
            new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = "9999999993",
                }
            });

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_nhsFhirClient.Object);
            x.AddSingleton(_matchingService.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = { { "99999999", "Dave", "Wilkes", "2000-04-01", "1", "ab12 3ed", "test@test.com", "0789 1234567" } }
        };

        await ReconciliationCsvFileProcessor.WriteCsvAsync(Path.Combine(_dir.IncomingDirectoryPath, inputData.TableName + ".csv"), inputData);

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
        Assert.NotNull(monitor.LastResult().Stats);

        _nhsFhirClient.Verify(x => x.PerformSearchByNhsId(It.IsAny<string>()), Times.Once, "The PerformSearchByNhsId method should have been invoked once");
        var outputData = await CsvFileMonitor.ReadCsvAsync(monitor.GetLastOperation().AssertSuccess().OutputCsvFile);

        Assert.Equal(nameof(ReconciliationStatus.LocalNhsNumberIsNotValid), outputData.Rows[0][ReconciliationCsvFileProcessor.HeaderStatus]);
    }

    [Fact]
    public async Task Reconciliation_ResponseIsNull()
    {
        // Arrange

        // Prepare instance of ReconciliationCsvFileProcessor
        var reconciliationCsvFileProcessor = new ReconciliationCsvFileProcessor(
            new Mock<ILogger<ReconciliationCsvFileProcessor>>().Object,
            new CsvMappingConfig(),
            new Mock<IMatchingService>().Object, // All responses are null
            Options.Create(new CsvWatcherConfig { SearchStrategy = "4" }));

        // Prepare input
        var inputData = new DataTable("file00001")
        {
            Columns =
            {
                TestDataHeaders.NhsNumber,
                TestDataHeaders.GivenName,
                TestDataHeaders.Surname,
                TestDataHeaders.DOB,
                TestDataHeaders.Gender,
                TestDataHeaders.PostCode,
                TestDataHeaders.Email,
                TestDataHeaders.Phone,
            },
            Rows = { { "", "Dave", "Wilkes", "2000-04-01", "1", "ab12 3ed", "test@test.com", "0789 1234567" } }
        };

        // Act
        var result = await reconciliationCsvFileProcessor.ProcessCsvFileAsync(inputData, _dir.ProcessedDirectoryPath);

        // Assert

        // Read Csv file and get values of all added columns
        var outputData = await CsvFileMonitor.ReadCsvAsync(result.OutputCsvFile);
        var columnsAddedByProcessing = outputData.Columns.Cast<DataColumn>()
            .Where(col => col.ColumnName.StartsWith("SUI_"));

        // All new columns should have empty values
        Assert.All(columnsAddedByProcessing, column =>
        {
            var fieldValue = outputData.Rows[0].Field<string>(column);
            Assert.Equal("-", fieldValue);
        });
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
        servicesCollection.AddSingleton<IMatchingService, MatchingServiceAdapter>(); // wires up the IMatchingService directly, without using http

        // core domain deps
        servicesCollection.AddSingleton<Shared.Endpoint.IMatchingService, MatchingService>();
        servicesCollection.AddSingleton<IReconciliationService, ReconciliationService>();
        servicesCollection.AddSingleton<IValidationService, ValidationService>();
        servicesCollection.AddSingleton<IAuditLogger, ChannelAuditLogger>();
        servicesCollection.AddSingleton(Channel.CreateUnbounded<AuditLogEntry>());
        servicesCollection.AddFeatureManagement();
        servicesCollection.AddSingleton<IConfiguration>(config);

        configure?.Invoke(servicesCollection);
        return servicesCollection.BuildServiceProvider();
    }

}