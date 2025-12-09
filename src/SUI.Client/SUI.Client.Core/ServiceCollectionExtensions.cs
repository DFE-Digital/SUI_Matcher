using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Shared.Logging;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Infrastructure.FileSystem;
using SUI.Client.Core.Infrastructure.Http;

namespace SUI.Client.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientCore(this IServiceCollection services, IConfiguration configuration, bool enableReconciliationMode)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole(options => options.FormatterName = Shared.SharedConstants.LogFormatter)
                .AddConsoleFormatter<LogConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddProvider(new JsonFileLoggerProvider(Path.Combine(Directory.GetCurrentDirectory(), enableReconciliationMode ? "sui-reconciliation-logs.json" : "sui-client-logs.json")));
        });

        var mapping = configuration.GetSection("CsvMapping").Get<CsvMappingConfig>() ?? new CsvMappingConfig();
        services.AddSingleton(mapping);
        if (enableReconciliationMode)
        {
            services.AddSingleton<ICsvFileProcessor, ReconciliationCsvFileProcessor>();
        }
        else
        {
            services.AddSingleton<ICsvFileProcessor, MatchingCsvFileProcessor>();
        }

        services.AddSingleton<IMatchingService, HttpApiMatchingService>();
        services.AddSingleton<CsvFileWatcherService>();
        services.AddSingleton<CsvFileMonitor>();
        return services;
    }
}