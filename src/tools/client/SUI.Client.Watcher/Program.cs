using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Shared.Extensions;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

Console.Out.WriteAppName("SUI CSV File Watcher");
Rule.Assert(args.Length == 3, "Usage: suiw <watch_directory> <output_directory> <matching_service_uri>");

var matchApiBaseAddress = args[2];
Rule.Assert(Uri.IsWellFormedUriString(matchApiBaseAddress, UriKind.Absolute),
    "Invalid URL format for matching service URL.");


var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration);
    services.AddLogging(configure => configure.AddConsole());
    services.Configure<CsvWatcherConfig>(x =>
    {
        x.IncomingDirectory = args[0];
        x.ProcessedDirectory = args[1];
    });
    services.AddHttpClient<IMatchPersonApiService, MatchPersonApiService>(client =>
    {
        client.BaseAddress = new Uri(matchApiBaseAddress);

        // Hack: until we can find a better way of routing for apps environment in azure.
        // Envoy uses SNI and needs a HOST header to route correctly
        // As we are using a private link, the host header needs to be set to the yarp hostname
        if (!matchApiBaseAddress.Contains("localhost"))
        {
            var uri = new Uri(matchApiBaseAddress);
            var yarpHostValue = uri.Host.Replace(".privatelink.", ".").TrimEnd('/');
            var hostUri = $"yarp.{yarpHostValue}";
            client.DefaultRequestHeaders.Add("Host", hostUri);
        }
    });
});

var host = builder.Build();
var cts = new CancellationTokenSource();
var processor = host.Services.GetRequiredService<CsvFileMonitor>();
var processingTask = processor.StartAsync(cts.Token);

const string commandHelpMessage = "Type 'q' to quit, 'stats' for statistics.";
Console.WriteLine($"File watcher started. {commandHelpMessage}");
bool holdAppOpen = true;
while (holdAppOpen)
{
    var command = Console.ReadLine();
    switch (command?.ToLower())
    {
        case "q":
            await cts.CancelAsync();
            holdAppOpen = false;
            break;
        case "stats":
            processor.PrintStats(Console.Out);
            continue;
        default:
            Console.WriteLine($"Unknown command. {commandHelpMessage}");
            continue;
    }
}

await processingTask;

await host.StopAsync();
cts.Dispose();