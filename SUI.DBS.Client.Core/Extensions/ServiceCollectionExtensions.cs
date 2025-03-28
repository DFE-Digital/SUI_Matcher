using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.DBS.Client.Core.Watcher;

namespace SUI.DBS.Client.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITxtFileProcessor, TxtFileProcessor>();
        services.AddSingleton<TxtFileWatcherService>();
        services.AddSingleton<TxtFileMonitor>();
        return services;
    }
}
