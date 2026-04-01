using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Endpoint;
using Shared.Models;
using SUI.Client.Core.Infrastructure.FileSystem;
using Xunit.Abstractions;

namespace Unit.Tests.Client.CsvProcessorReconciliationTests;

using D = Dictionary<string, string>;

public class CsvProcessorMissingFieldsTests(ITestOutputHelper testOutputHelper)
    : CsvProcessorTestBase(testOutputHelper)
{
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<IMatchingService> _matchingService = new(MockBehavior.Loose);

    [Fact]
    public async Task Reconciliation_CsvHeaderDifferences_ShouldShowDifferences()
    {
        var demographicResult = new DemographicResult
        {
            Result = new NhsPerson
            {
                NhsNumber = "9449305552",
                GivenNames = ["John"],
                FamilyNames = ["Smithy"],
                BirthDate = new DateOnly(2000, 04, 01),
                Gender = "Male",
                AddressPostalCodes = ["ab12 3ed"],
                Emails = ["test@test.com"],
                PhoneNumbers = ["0789 1234567"],
            },
        };

        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult
                    {
                        MatchStatus = MatchStatus.Match,
                        NhsNumber = "9449305552",
                    },
                }
            );
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(It.IsAny<string>()))
            .Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(
            true,
            x =>
            {
                x.AddSingleton(_matchingService.Object);
                x.AddSingleton(_nhsFhirClient.Object);
            }
        );
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "9449305551",
            [TestDataHeaders.GivenName] = "Dave",
            [TestDataHeaders.Surname] = "Wilkes",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(
            Path.Combine(_tempDir.IncomingDirectoryPath, "file00001.csv"),
            headers,
            list
        );

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync(); // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        AssertCsvFileRunSuccessfully(monitor);

        _nhsFhirClient.Verify(
            x => x.PerformSearchByNhsId(It.IsAny<string>()),
            Times.Once,
            "The PerformSearchByNhsId method should have been invoked once"
        );
        (_, List<D> records) = await CsvRecordReader.ReadCsvFileAsync(
            monitor.GetLastOperation().AssertSuccess().OutputCsvFile
        );

        Assert.Equal(
            "NhsNumber - Given - Family",
            records[0][ReconciliationCsvFileProcessor.HeaderDifferences]
        );
    }

    [Fact]
    public async Task Reconciliation_CsvHeaderMissingLocalFields_ShouldWritLocaleMissingFields_WhenThereAreMissingFields()
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
            },
        };
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.NoMatch },
                }
            );
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(It.IsAny<string>()))
            .Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(
            true,
            x =>
            {
                x.AddSingleton(_matchingService.Object);
                x.AddSingleton(_nhsFhirClient.Object);
            }
        );
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "",
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(
            Path.Combine(_tempDir.IncomingDirectoryPath, "file00001.csv"),
            headers,
            list
        );

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync(); // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        AssertCsvFileRunSuccessfully(monitor);

        (_, List<D> records) = await CsvRecordReader.ReadCsvFileAsync(
            monitor.GetLastOperation().AssertSuccess().OutputCsvFile
        );

        Assert.Equal(
            "NhsNumber - Gender",
            records[0][ReconciliationCsvFileProcessor.HeaderMissingLocalFields]
        );
    }

    [Fact]
    public async Task Reconciliation_CsvHeaderMissingNhsFields_ShouldWriteNhsMissingFields_WhenThereAreMissingFields()
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
                Emails = [],
                PhoneNumbers = [],
            },
        };
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult
                    {
                        MatchStatus = MatchStatus.Match,
                        NhsNumber = demographicResult.Result.NhsNumber,
                    },
                }
            );
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(It.IsAny<string>()))
            .Returns(() => Task.FromResult(demographicResult));

        var cts = new CancellationTokenSource();
        var provider = Bootstrap(
            true,
            x =>
            {
                x.AddSingleton(_matchingService.Object);
                x.AddSingleton(_nhsFhirClient.Object);
            }
        );
        var monitor = provider.GetRequiredService<CsvFileMonitor>();
        var monitoringTask = monitor.StartAsync(cts.Token);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var data = new D
        {
            [TestDataHeaders.NhsNumber] = "",
            [TestDataHeaders.GivenName] = "John",
            [TestDataHeaders.Surname] = "Smith",
            [TestDataHeaders.DOB] = "2000-04-01",
            [TestDataHeaders.PostCode] = "ab12 3ed",
            [TestDataHeaders.Email] = "test@test.com",
            [TestDataHeaders.Phone] = "0789 1234567",
        };

        var list = new List<D> { data };
        var headers = new HashSet<string>(data.Keys);

        await ReconciliationCsvFileProcessor.WriteCsvAsync(
            Path.Combine(_tempDir.IncomingDirectoryPath, "file00001.csv"),
            headers,
            list
        );

        monitor.Processed += (_, _) => tcs.SetResult();
        await tcs.Task; // await processing of that file
        await cts.CancelAsync(); // cancel the task
        await monitoringTask; // await cancellation

        // ASSERTS
        AssertCsvFileRunSuccessfully(monitor);

        _nhsFhirClient.Verify(
            x => x.PerformSearchByNhsId(It.IsAny<string>()),
            Times.Once,
            "The PerformSearchByNhsId method should have been invoked once"
        );
        (_, List<D> records) = await CsvRecordReader.ReadCsvFileAsync(
            monitor.GetLastOperation().AssertSuccess().OutputCsvFile
        );

        Assert.Equal(
            "Email - Phone",
            records[0][ReconciliationCsvFileProcessor.HeaderMissingNhsFields]
        );
    }
}
