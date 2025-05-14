using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SUI.DBS.Response.Logger.Core;
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

Console.WriteLine("File watcher started. Type 'q' to quit, 'stats' for statistics.");
while (true)
{
    var command = Console.ReadLine();
    if (command == "q")
    {
        cts.Cancel();
        break;
    }
    else if (command == "stats")
    {
        processor.PrintStats(Console.Out);
    }
}

await processingTask;

await host.StopAsync();