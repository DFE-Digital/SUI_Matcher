using System.Diagnostics.CodeAnalysis;

using SUI.Client.Core.Infrastructure.FileSystem;

namespace SUI.Client.Service.Watcher;

[ExcludeFromCodeCoverage(Justification = "Nothing to test")]
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CsvFileMonitor _csvFileMonitor;

    public Worker(ILogger<Worker> logger, CsvFileMonitor csvFileMonitor)
    {
        _logger = logger;
        _csvFileMonitor = csvFileMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
            }

            await _csvFileMonitor.StartAsync(stoppingToken);
        }
    }
}