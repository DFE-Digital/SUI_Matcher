using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shared.Logging;

using SUI.DBS.Response.Logger.Core.Watcher;

namespace SUI.DBS.Response.Logger.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole(options => options.FormatterName = "log4net")
                .AddConsoleFormatter<LogConsoleFormatter, CustomOptions>();
            builder.AddProvider(new JsonFileLoggerProvider(Path.Combine(Directory.GetCurrentDirectory(), "logs.json")));
        });

        services.AddSingleton<ITxtFileProcessor, TxtFileProcessor>();
        services.AddSingleton<TxtFileWatcherService>();
        services.AddSingleton<TxtFileMonitor>();
        return services;
    }
}