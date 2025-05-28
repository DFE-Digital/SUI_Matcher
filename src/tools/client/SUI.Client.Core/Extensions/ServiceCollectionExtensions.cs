using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shared.Logging;

using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

namespace SUI.Client.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientCore(this IServiceCollection services, IConfiguration configuration, string matchApiBaseAddress)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole(options => options.FormatterName = "log4net")
                .AddConsoleFormatter<LogConsoleFormatter, CustomOptions>();
            builder.AddProvider(new JsonFileLoggerProvider(Path.Combine(Directory.GetCurrentDirectory(), "sui-client-logs.json")));
        });

        var mapping = configuration.GetSection("CsvMapping").Get<CsvMappingConfig>() ?? new CsvMappingConfig();

        services.AddSingleton(mapping);
        services.AddSingleton(x => new HttpClient() { BaseAddress = new Uri(matchApiBaseAddress) });
        services.AddSingleton<ICsvFileProcessor, CsvFileProcessor>();
        services.AddSingleton<IMatchPersonApiService, MatchPersonApiService>();
        services.AddSingleton<CsvFileWatcherService>();
        services.AddSingleton<CsvFileMonitor>();
        return services;
    }
}