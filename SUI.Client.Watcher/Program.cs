using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.Client.Core;
using SUI.Client.Core.Extensions;
using SUI.Client.Watcher;

Console.Out.WriteAppName("SUI CSV File Watcher");
Rule.Assert(args.Length == 2, "Usage: suiw <watch_directory> <output_directory>");

var appConfig = new CsvWatcherConfig
{
    IncomingDirectory = args[0],
    ProcessedDirectory = args[1]
};

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration);
    services.AddSingleton(appConfig);
    services.AddLogging(configure => configure.AddConsole()); 
    services.AddSingleton<CsvFileWatcherService>();
    services.AddSingleton<CsvFileMonitor>();
});

var host = builder.Build();
var cts = new CancellationTokenSource();
var processor = host.Services.GetRequiredService<CsvFileMonitor>();
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