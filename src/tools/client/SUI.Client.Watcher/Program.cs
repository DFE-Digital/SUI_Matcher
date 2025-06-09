using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Shared.Extensions;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Watcher;

Console.Out.WriteAppName("SUI CSV File Watcher");
Rule.Assert(args.Length == 3, "Usage: suiw <watch_directory> <output_directory> <matching_service_uri>");
Rule.Assert(Uri.IsWellFormedUriString(args[2], UriKind.Absolute), "Invalid URL format for matching service URL.");
var matchApiBaseAddress = args[2];

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration, matchApiBaseAddress);
    services.AddLogging(configure => configure.AddConsole());
    services.Configure<CsvWatcherConfig>(x =>
    {
        x.IncomingDirectory = args[0];
        x.ProcessedDirectory = args[1];
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