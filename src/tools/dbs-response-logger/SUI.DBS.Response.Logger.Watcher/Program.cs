using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shared.Extensions;
using Shared.Util;

using SUI.DBS.Response.Logger.Core.Extensions;
using SUI.DBS.Response.Logger.Core.Watcher;

Console.Out.WriteAppName("DBS Txt File Watcher");
Rule.Assert(args.Length == 2, "Usage: dbsw <watch_directory>");

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration);
    services.Configure<TxtWatcherConfig>(x =>
    {
        x.IncomingDirectory = args[0];
        x.ProcessedDirectory = args[1];
    });
});

var host = builder.Build();
var cts = new CancellationTokenSource();
var processor = host.Services.GetRequiredService<TxtFileMonitor>();
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