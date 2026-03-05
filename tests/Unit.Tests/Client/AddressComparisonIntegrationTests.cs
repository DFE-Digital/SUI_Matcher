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

using SUI.Client.Core;
using SUI.Client.Core.Infrastructure.FileSystem;

using Unit.Tests.Util;
using Unit.Tests.Util.Adapters;

using Xunit.Abstractions;

using D = System.Collections.Generic.Dictionary<string, string>;
using IMatchingService = SUI.Client.Core.Application.Interfaces.IMatchingService;

namespace Unit.Tests.Client;

public class AddressComparisonIntegrationTests(ITestOutputHelper testOutputHelper)
{
    private readonly TempDirectoryFixture _dir = new();
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<Shared.Endpoint.IMatchingService> _matchingService = new(MockBehavior.Loose);

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
        public const string Phone = "Phone";
        public const string AddressHistory = "AddressHistory";
    }

    /// <summary>
    /// Lots of test code to verify that the address comparison stats are being tracked and calculated correctly across a variety of scenarios, including:
    /// - Exact matches
    /// - Partial matches (e.g. house number + postcode match in histories)
    /// - Local postcode matches in histories
    /// - No matches
    /// </summary>
    /// <exception cref="Exception"></exception>
    [Fact]
    public async Task AddressComparisonStats_ShouldBeTrackedCorrectly()
    {
        // ARRANGE - 10 test records with various address scenarios
        var testRecords = SetupTestData();

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = testRecords.Select(x => x.cmsData).ToList();
        var headers = new HashSet<string>(data[0].Keys);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(
            Path.Combine(_dir.IncomingDirectoryPath, "address_stats_test.csv"),
            headers,
            data);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task;
        await cts.CancelAsync();
        await monitoringTask;

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

        var stats = await ReconciliationCsvFileProcessor.ReadStatsJsonFileAsync(
            monitor.GetLastOperation().AssertSuccess().StatsJsonFile);

        // Verify address comparison stats
        Assert.True(stats.ContainsKey("PrimaryAddressSame"), "Stats should contain PrimaryAddressSame");
        Assert.True(stats.ContainsKey("PrimaryAddressSamePercentage"), "Stats should contain PrimaryAddressSamePercentage");
        Assert.True(stats.ContainsKey("AddressHistoriesIntersect"), "Stats should contain AddressHistoriesIntersect");
        Assert.True(stats.ContainsKey("AddressHistoriesIntersectPercentage"), "Stats should contain AddressHistoriesIntersectPercentage");
        Assert.True(stats.ContainsKey("PrimaryCMSAddressInPDSHistory"), "Stats should contain PrimaryCMSAddressInPDSHistory");
        Assert.True(stats.ContainsKey("PrimaryCMSAddressInPDSHistoryPercentage"), "Stats should contain PrimaryCMSAddressInPDSHistoryPercentage");
        Assert.True(stats.ContainsKey("PrimaryPDSAddressInCMSHistory"), "Stats should contain PrimaryPDSAddressInCMSHistory");
        Assert.True(stats.ContainsKey("PrimaryPDSAddressInCMSHistoryPercentage"), "Stats should contain PrimaryPDSAddressInCMSHistoryPercentage");

        // Expected counts:
        Assert.Equal(4, stats.GetValueOrDefault("PrimaryAddressSame"));
        Assert.Equal(40, stats.GetValueOrDefault("PrimaryAddressSamePercentage"));

        Assert.Equal(7, stats.GetValueOrDefault("AddressHistoriesIntersect"));
        Assert.Equal(70, stats.GetValueOrDefault("AddressHistoriesIntersectPercentage"));


        Assert.Equal(7, stats.GetValueOrDefault("PrimaryCMSAddressInPDSHistory"));
        Assert.Equal(70, stats.GetValueOrDefault("PrimaryCMSAddressInPDSHistoryPercentage"));

        Assert.Equal(6, stats.GetValueOrDefault("PrimaryPDSAddressInCMSHistory"));
        Assert.Equal(60, stats.GetValueOrDefault("PrimaryPDSAddressInCMSHistoryPercentage"));
    }

    [Fact]
    public async Task AddressComparison_ShouldWriteTrueWhereComparison()
    {
        // ARRANGE - 1 test record with all address comparison scenarios true
        var testRecord = SetupTestData().First();


        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new List<D> { testRecord.cmsData };
        var headers = new HashSet<string>(data[0].Keys);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(
            Path.Combine(_dir.IncomingDirectoryPath, "address_comparison_row_test.csv"),
            headers,
            data);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task;
        await cts.CancelAsync();
        await monitoringTask;

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));

        (_, List<D> records) = await ReconciliationCsvFileProcessor.ReadCsvAsync(monitor.LastResult().OutputCsvFile);
        Assert.NotNull(records);

        var record = records!.First();
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderPrimaryAddressSame), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderPrimaryAddressSame} column");
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderAddressHistoriesIntersect), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderAddressHistoriesIntersect} column");
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderPrimaryCMSAddressInPDSHistory), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderPrimaryCMSAddressInPDSHistory} column");
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderPrimaryPDSAddressInCMSHistory), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderPrimaryPDSAddressInCMSHistory} column");

        // Happy path assertions
        Assert.Equal("True", record.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderPrimaryAddressSame));
        Assert.Equal("True", record.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderAddressHistoriesIntersect));
        Assert.Equal("True", record.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderPrimaryCMSAddressInPDSHistory));
        Assert.Equal("True", record.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderPrimaryPDSAddressInCMSHistory));
    }

    [Fact]
    public async Task AddressComparison_ShouldWriteFalseWhereNoComparison()
    {
        // ARRANGE - 1 test record with all address comparison scenarios true
        var testRecord = SetupTestData().Last();

        // ACT
        var cts = new CancellationTokenSource();
        var provider = Bootstrap(true, x =>
        {
            x.AddSingleton(_matchingService.Object);
            x.AddSingleton(_nhsFhirClient.Object);
        });
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new List<D> { testRecord.cmsData };
        var headers = new HashSet<string>(data[0].Keys);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(
            Path.Combine(_dir.IncomingDirectoryPath, "address_comparison_row_test.csv"),
            headers,
            data);

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task;
        await cts.CancelAsync();
        await monitoringTask;

        // ASSERTS
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }

        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(1, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));

        (_, List<D> records) = await ReconciliationCsvFileProcessor.ReadCsvAsync(monitor.LastResult().OutputCsvFile);
        Assert.NotNull(records);

        var record = records!.First();
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderPrimaryAddressSame), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderPrimaryAddressSame} column");
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderAddressHistoriesIntersect), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderAddressHistoriesIntersect} column");
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderPrimaryCMSAddressInPDSHistory), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderPrimaryCMSAddressInPDSHistory} column");
        Assert.True(record.ContainsKey(ReconciliationCsvFileProcessor.HeaderPrimaryPDSAddressInCMSHistory), $"Output CSV should contain {ReconciliationCsvFileProcessor.HeaderPrimaryPDSAddressInCMSHistory} column");


        var lastRecord = records.Last();
        Assert.Equal("False", lastRecord.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderPrimaryAddressSame));
        Assert.Equal("False", lastRecord.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderAddressHistoriesIntersect));
        Assert.Equal("False", lastRecord.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderPrimaryCMSAddressInPDSHistory));
        Assert.Equal("False", lastRecord.GetValueOrDefault(ReconciliationCsvFileProcessor.HeaderPrimaryPDSAddressInCMSHistory));

    }


    private List<(D cmsData, DemographicResult nhsData)> SetupTestData()
    {
        var data = new List<(D cmsData, DemographicResult nhsData)>
        {
            // Record 1: PrimaryCMSAddressInPDSHistory + PrimaryAddressSame + AddressHistoriesIntersect = true (exact match)
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305552",
                    [TestDataHeaders.GivenName] = "John",
                    [TestDataHeaders.Surname] = "Smith",
                    [TestDataHeaders.DOB] = "2000-04-01",
                    [TestDataHeaders.Gender] = "1",
                    [TestDataHeaders.PostCode] = "LS12 3EA",
                    [TestDataHeaders.Email] = "test1@test.com",
                    [TestDataHeaders.Phone] = "0789 1111111",
                    [TestDataHeaders.AddressHistory] = "home~64 Higher Street~Leeds~West Yorkshire~LS12 3EA|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305552",
                        GivenNames = ["John"],
                        FamilyNames = ["Smith"],
                        BirthDate = new DateOnly(2000, 04, 01),
                        Gender = "Male",
                        AddressPostalCodes = ["LS12 3EA"],
                        AddressHistory = ["home~64 Higher Street~Leeds~West Yorkshire~LS12 3EA|"]
                    }
                }
            ),
            // Record 2: PrimaryAddressSame + PrimaryCMSAddressInPDSHistory + AddressHistoriesIntersect = true (house number + postcode match in histories)
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305553",
                    [TestDataHeaders.GivenName] = "Jane",
                    [TestDataHeaders.Surname] = "Doe",
                    [TestDataHeaders.DOB] = "1995-05-15",
                    [TestDataHeaders.Gender] = "2",
                    [TestDataHeaders.PostCode] = "LS12 3EC",
                    [TestDataHeaders.Email] = "test2@test.com",
                    [TestDataHeaders.AddressHistory] = "current~64 Lower Street~Leeds~West Yorkshire~LS12 3EC|previous~10 Old Street~Leeds~West Yorkshire~LS12 3EZ|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305553",
                        GivenNames = ["Jane"],
                        FamilyNames = ["Doe"],
                        BirthDate = new DateOnly(1995, 05, 15),
                        Gender = "Female",
                        AddressPostalCodes = ["LS12 3EC"],
                        AddressHistory = ["home~64 Lower Street~Leeds~West Yorkshire~LS12 3EC|", "previous~10 Old Street~Leeds~West Yorkshire~LS12 3EZ|"]
                    }
                }
            ),
            // Record 3: PrimaryCMSAddressInPDSHistory + AddressHistoriesIntersect = true (local postcode in NHS history)
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305554",
                    [TestDataHeaders.GivenName] = "Bob",
                    [TestDataHeaders.Surname] = "Johnson",
                    [TestDataHeaders.DOB] = "1985-08-20",
                    [TestDataHeaders.Gender] = "1",
                    [TestDataHeaders.PostCode] = "M11AA",
                    [TestDataHeaders.Email] = "test3@test.com",
                    [TestDataHeaders.AddressHistory] = "current~20 New Street~Manchester~M12BB|previous~15 Old Road~Manchester~M11AA|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305554",
                        GivenNames = ["Bob"],
                        FamilyNames = ["Johnson"],
                        BirthDate = new DateOnly(1985, 08, 20),
                        Gender = "Male",
                        AddressPostalCodes = ["M12BB"],
                        AddressHistory = ["current~20 New Street~Manchester~M12BB|", "previous~15 Old Road~Manchester~M11AA|"]
                    }
                }
            ),
            // Record 4: PrimaryCMSAddressInPDSHistory + AddressHistoriesIntersect = true (local postcode in NHS history)
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305555",
                    [TestDataHeaders.GivenName] = "Alice",
                    [TestDataHeaders.Surname] = "Williams",
                    [TestDataHeaders.DOB] = "1992-12-10",
                    [TestDataHeaders.Gender] = "2",
                    [TestDataHeaders.PostCode] = "SE1 1AA",
                    [TestDataHeaders.Email] = "test4@test.com",
                    [TestDataHeaders.AddressHistory] = "1~40 High Street~London~SE1 2BB|2~30 Main St~London~SE1 1AA|",
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305555",
                        GivenNames = ["Alice"],
                        FamilyNames = ["Williams"],
                        BirthDate = new DateOnly(1992, 12, 10),
                        Gender = "Female",
                        AddressPostalCodes = ["SE1 2BB"],
                        AddressHistory = ["previous~30 Main St~London~SE1 1AA|", "current~40 High Street~London~SE1 2BB|"]
                    }
                }
            ),
            // Record 5: Multiple matches - PrimaryAddressSame + PrimaryCMSAddressInPDSHistory + AddressHistoriesIntersect
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305556",
                    [TestDataHeaders.GivenName] = "Charlie",
                    [TestDataHeaders.Surname] = "Brown",
                    [TestDataHeaders.DOB] = "1988-03-25",
                    [TestDataHeaders.Gender] = "1",
                    [TestDataHeaders.PostCode] = "B11CC",
                    [TestDataHeaders.Email] = "test5@test.com",
                    [TestDataHeaders.AddressHistory] = "current~50 Park Lane~Birmingham~B11CC|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305556",
                        GivenNames = ["Charlie"],
                        FamilyNames = ["Brown"],
                        BirthDate = new DateOnly(1988, 03, 25),
                        Gender = "Male",
                        AddressPostalCodes = ["B11CC"],
                        AddressHistory = ["home~50 Park Lane~Birmingham~B11CC|", "work~60 Office St~Birmingham~B12DD|"]
                    }
                }
            ),
            // Record 6: No address matches
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305557",
                    [TestDataHeaders.GivenName] = "David",
                    [TestDataHeaders.Surname] = "Taylor",
                    [TestDataHeaders.DOB] = "1990-07-30",
                    [TestDataHeaders.Gender] = "1",
                    [TestDataHeaders.PostCode] = "G11AA",
                    [TestDataHeaders.Email] = "test6@test.com",
                    [TestDataHeaders.AddressHistory] = "1~70 Different St~Glasgow~G11AA|"

                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305557",
                        GivenNames = ["David"],
                        FamilyNames = ["Taylor"],
                        BirthDate = new DateOnly(1990, 07, 30),
                        Gender = "Male",
                        AddressPostalCodes = ["G12BB"],
                        AddressHistory = ["home~71 Different St~Glasgow~G13CC|"]
                    }
                }
            ),
            // Record 7: PrimaryCMSAddressInPDSHistory + AddressHistoriesIntersect x2
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305558",
                    [TestDataHeaders.GivenName] = "Emma",
                    [TestDataHeaders.Surname] = "Wilson",
                    [TestDataHeaders.DOB] = "1998-11-05",
                    [TestDataHeaders.Gender] = "2",
                    [TestDataHeaders.PostCode] = "BS1 1AA",
                    [TestDataHeaders.Email] = "test7@test.com",
                    [TestDataHeaders.AddressHistory] = "current~80 Garden Ave~Bristol~BS1 1AA|previous~90 Previous Rd~Bristol~BS1 3CC|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305558",
                        GivenNames = ["Emma"],
                        FamilyNames = ["Wilson"],
                        BirthDate = new DateOnly(1998, 11, 05),
                        Gender = "Female",
                        AddressPostalCodes = ["BS1 2BB"],
                        AddressHistory = ["current~80 Garden Ave~Bristol~BS1 1AA|", "old~90 Previous Rd~Bristol~BS1 3CC|"]
                    }
                }
            ),
            // Record 8: PrimaryAddressSame +  PrimaryCMSAddressInPDSHistory + PrimaryPDSAddressInCMSHistory + AddressHistoriesIntersect + Both History checks
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305559",
                    [TestDataHeaders.GivenName] = "Frank",
                    [TestDataHeaders.Surname] = "Miller",
                    [TestDataHeaders.DOB] = "1982-02-14",
                    [TestDataHeaders.Gender] = "1",
                    [TestDataHeaders.PostCode] = "EH1 1AA",
                    [TestDataHeaders.Email] = "test8@test.com",
                    [TestDataHeaders.AddressHistory] = "1~100 Royal Mile~Edinburgh~EH1 1AA|previous~200 Old Town~Edinburgh~EH1 2BB|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305559",
                        GivenNames = ["Frank"],
                        FamilyNames = ["Miller"],
                        BirthDate = new DateOnly(1982, 02, 14),
                        Gender = "Male",
                        AddressPostalCodes = ["EH1 1AA"],
                        AddressHistory = ["home~100 Royal Mile~Edinburgh~EH1 1AA|"]
                    }
                }
            ),
            // Record 9: Empty local address
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305560",
                    [TestDataHeaders.GivenName] = "Grace",
                    [TestDataHeaders.Surname] = "Davis",
                    [TestDataHeaders.DOB] = "1996-09-22",
                    [TestDataHeaders.Gender] = "2",
                    [TestDataHeaders.PostCode] = "",
                    [TestDataHeaders.Email] = "test9@test.com",
                    [TestDataHeaders.AddressHistory] = ""
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305560",
                        GivenNames = ["Grace"],
                        FamilyNames = ["Davis"],
                        BirthDate = new DateOnly(1996, 09, 22),
                        Gender = "Female",
                        AddressPostalCodes = ["CF1 1AA"],
                        AddressHistory = ["home~110 Castle St~Cardiff~CF1 1AA|"]
                    }
                }
            ),
            // Record 10: No matches (different postcodes)
            (
                new D
                {
                    [TestDataHeaders.NhsNumber] = "9449305561",
                    [TestDataHeaders.GivenName] = "Henry",
                    [TestDataHeaders.Surname] = "Moore",
                    [TestDataHeaders.DOB] = "1987-06-18",
                    [TestDataHeaders.Gender] = "1",
                    [TestDataHeaders.PostCode] = "NR1 1AA",
                    [TestDataHeaders.Email] = "test10@test.com",
                    [TestDataHeaders.AddressHistory] = "current~130 Modern Street~Norwich~NR1 1AA|"
                },
                new DemographicResult
                {
                    Result = new NhsPerson
                    {
                        NhsNumber = "9449305561",
                        GivenNames = ["Henry"],
                        FamilyNames = ["Moore"],
                        BirthDate = new DateOnly(1987, 06, 18),
                        Gender = "Male",
                        AddressPostalCodes = ["NR12BB"],
                        AddressHistory = ["home~130 Modern Street~Norwich~NR1 2BB|"]
                    }
                }
            )
        };

        foreach (var (cmsData, nhsData) in data)
        {
            _nhsFhirClient
                .Setup(x => x.PerformSearchByNhsId(cmsData[TestDataHeaders.NhsNumber]))
                .ReturnsAsync(nhsData);

            _matchingService
                .Setup(x => x.SearchAsync(
                    It.Is<SearchSpecification>(s => s.Email == cmsData[TestDataHeaders.Email]),
                    false))
                .ReturnsAsync(new PersonMatchResponse
                {
                    Result = new MatchResult
                    {
                        MatchStatus = MatchStatus.Match,
                        NhsNumber = nhsData.Result!.NhsNumber,
                        Score = 1
                    }
                });
        }

        return data;
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
        servicesCollection.AddSingleton<IMatchingService, MatchingServiceAdapter>();

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