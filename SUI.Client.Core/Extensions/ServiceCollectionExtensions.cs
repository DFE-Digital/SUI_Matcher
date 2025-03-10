using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

namespace SUI.Client.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientCore(this IServiceCollection services, IConfiguration configuration)
    {
        var apiBaseAddress = configuration["MatchApiBaseAddress"] ?? throw new Exception("Config item 'MatchApiBaseAddress' not set");
        var mapping = configuration.GetSection("CsvMapping").Get<CsvMappingConfig>() ?? new CsvMappingConfig();

        services.AddSingleton(mapping);
        services.AddSingleton(x => new HttpClient() { BaseAddress = new Uri(apiBaseAddress) });
        services.AddSingleton<ICsvFileProcessor, CsvFileProcessor>();
        services.AddSingleton<IMatchPersonApiService, MatchPersonApiService>();
        services.AddSingleton<CsvFileWatcherService>();
        services.AddSingleton<CsvFileMonitor>();
        return services;
    }
}
