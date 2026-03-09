using System.Threading.Channels;

using MatchingApi.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

using Shared.Endpoint;
using Shared.Logging;

using SUI.Client.Core;
using SUI.Client.Core.Infrastructure.FileSystem;

using Unit.Tests.Util;
using Unit.Tests.Util.Adapters;

using Xunit.Abstractions;

using IMatchingService = SUI.Client.Core.Application.Interfaces.IMatchingService;

namespace Unit.Tests.Client.CsvProcessorReconciliationTests;

public class CsvProcessorTestBase(ITestOutputHelper testOutputHelper)
{
    public required ITestOutputHelper TestContext = testOutputHelper;
    protected readonly TempDirectoryFixture _tempDir = new();
    
    protected static class TestDataHeaders
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
    
    protected ServiceProvider Bootstrap(bool enableReconciliation, Action<ServiceCollection>? configure = null)
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddLogging(b => b.AddDebug().AddProvider(new TestContextLoggerProvider(TestContext)));
        var config = new ConfigurationBuilder()
            .Build();

        servicesCollection.Configure<CsvWatcherConfig>(x =>
        {
            x.IncomingDirectory = _tempDir.IncomingDirectoryPath;
            x.ProcessedDirectory = _tempDir.ProcessedDirectoryPath;
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

    public void AssertCsvFileRunSuccessfully(CsvFileMonitor monitor, int processedCount = 1)
    {
        if (monitor.GetLastOperation().Exception != null)
        {
            throw monitor.GetLastOperation().Exception!;
        }
        
        Assert.Null(monitor.GetLastOperation().Exception);
        Assert.Equal(0, monitor.ErrorCount);
        Assert.Equal(processedCount, monitor.ProcessedCount);
        Assert.True(File.Exists(monitor.LastResult().OutputCsvFile));
        Assert.True(File.Exists(monitor.LastResult().StatsJsonFile));
        Assert.NotNull(monitor.LastResult().Stats);
    }
}