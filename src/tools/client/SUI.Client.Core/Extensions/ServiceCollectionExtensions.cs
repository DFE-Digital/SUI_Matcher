using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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
                .AddConsoleFormatter<LogConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddProvider(new JsonFileLoggerProvider(Path.Combine(Directory.GetCurrentDirectory(), "sui-client-logs.json")));
        });

        var mapping = configuration.GetSection("CsvMapping").Get<CsvMappingConfig>() ?? new CsvMappingConfig();

        services.AddSingleton(mapping);
        services.AddSingleton(x =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(matchApiBaseAddress)
            };

            // Hack: until we can find a better way of routing for apps environment in azure, Envoy uses SNI and needs a HOST header to route correctly
            var hostUri = matchApiBaseAddress;
            if (!hostUri.Contains("localhost"))
            {
                var uri = new Uri(matchApiBaseAddress);
                var removeBits = uri.Host.Replace(".privatelink.", ".").TrimEnd('/');
                hostUri = $"yarp.{removeBits}";
                Console.WriteLine(hostUri);
                client.DefaultRequestHeaders.Add("Host", $"{hostUri}");
            }
            // Add more headers as needed
            return client;
        });
        services.AddSingleton<ICsvFileProcessor, CsvFileProcessor>();
        services.AddSingleton<IMatchPersonApiService, MatchPersonApiService>();
        services.AddSingleton<CsvFileWatcherService>();
        services.AddSingleton<CsvFileMonitor>();
        return services;
    }
}