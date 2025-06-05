using System.Globalization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

using Shared.Extensions;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Watcher;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("sui-client-watcher.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Console.Out.WriteAppName("SUI CSV File Watcher");
Rule.Assert(args.Length == 3, "Usage: suiw <watch_directory> <output_directory> <matching_service_uri>");
Rule.Assert(Uri.IsWellFormedUriString(args[2], UriKind.Absolute), "Invalid URL format for matching service URL.");
var matchApiBaseAddress = args[2];

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration, matchApiBaseAddress);
    // Replace AddConsole with Serilog. Temporary for now. If it works well, we can add it to the core library.
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog();
    });
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

Console.WriteLine("File watcher started. Type 'q' to quit, 'stats' for statistics.");
while (true)
{
    var command = Console.ReadLine();
    if (command == "q")
    {
        await cts.CancelAsync();
        break;
    }
    else if (command == "stats")
    {
        processor.PrintStats(Console.Out);
    }
}

await processingTask;

await host.StopAsync();
cts.Dispose();

// Ensure to flush and close Serilog
Log.CloseAndFlush();
