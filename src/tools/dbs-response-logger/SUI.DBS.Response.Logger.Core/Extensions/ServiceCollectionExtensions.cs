using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Shared.Logging;

using SUI.DBS.Response.Logger.Core.Watcher;

namespace SUI.DBS.Response.Logger.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole(options => options.FormatterName = "custom-formatter")
                .AddConsoleFormatter<LogConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddProvider(new JsonFileLoggerProvider(Path.Combine(Directory.GetCurrentDirectory(), "dbs-response-logger-logs.json")));
        });

        services.AddSingleton<ITxtFileProcessor, TxtFileProcessor>();
        services.AddSingleton<TxtFileWatcherService>();
        services.AddSingleton<TxtFileMonitor>();
        return services;
    }
}