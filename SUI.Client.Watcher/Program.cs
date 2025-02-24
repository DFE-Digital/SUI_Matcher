using SUI.Client.Watcher;


var config = new AppConfig();
ILogger logger = new ConsoleFileLogger(Console.Out, config);
using FileWatcherService fileWatcherService = new(config, logger);
Processor processor = new(fileWatcherService, config, logger);
CancellationTokenSource cts = new();

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
