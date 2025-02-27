using SUI.Client.Core;
using SUI.Client.Watcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.Client.Core.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

const string Name = "********** SUI CSV File Watcher **********";
var p = new string('*', Name.Length);

Console.WriteLine(p);
Console.WriteLine(Name);
Console.WriteLine(p);
Console.WriteLine();

if (args.Length < 2)
{
    Console.WriteLine("Usage: suiw <watch_directory> <output_directory>");
    return;
}

var appConfig = new AppConfig
{
    IncomingDirectory = args[0],
    ProcessedDirectory = args[1]
};

var builder = Host.CreateDefaultBuilder();

builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.SetBasePath(AppContext.BaseDirectory);
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
});

builder.ConfigureServices((hostContext, services) =>
{
    var apiBaseAddress = hostContext.Configuration["MatchApiBaseAddress"] ?? throw new Exception("Config item 'MatchApiBaseAddress' not set");
    var mapping = hostContext.Configuration.GetSection("CsvMapping").Get<CsvMappingConfig>() ?? new CsvMappingConfig();

    services.AddSingleton(appConfig);
    services.AddSingleton(mapping);
    services.AddLogging(configure => configure.AddConsole());
    services.AddSingleton(x => new HttpClient() { BaseAddress = new Uri(apiBaseAddress) });

    services.AddSingleton<IFileProcessor, FileProcessor>();
    services.AddSingleton<FileWatcherService>();
    services.AddSingleton<Processor>();
    services.AddSingleton<IMatchPersonApiService, MatchPersonApiService>();
    services.AddSingleton<IFileProcessor, FileProcessor>();
});


var host = builder.Build();

CancellationTokenSource cts = new();
var processor = host.Services.GetRequiredService<Processor>();
var processingTask = processor.StartAsync(CancellationToken.None);

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
