using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SUI.DBS.Client.Core.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureAppSettingsJsonFile(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(AppContext.BaseDirectory);
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        });
    }
}