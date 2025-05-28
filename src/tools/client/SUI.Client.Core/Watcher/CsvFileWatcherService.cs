using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SUI.Client.Core.Watcher;

/// <summary>
/// Wrapper for FileSystemWatcher
/// </summary>
public class CsvFileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger _logger;
    public event EventHandler<string>? FileDetected;

    public int Count { get; private set; }

    public CsvFileWatcherService(IOptions<CsvWatcherConfig> config, ILoggerFactory loggerFactory)
    {
        CsvWatcherConfig config1 = config.Value;
        _logger = loggerFactory.CreateLogger<CsvFileWatcherService>();
        Directory.CreateDirectory(config1.IncomingDirectory);
        _watcher = new FileSystemWatcher(config1.IncomingDirectory, "*.csv")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        _watcher.Created += OnCreated;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        Count++;
        _logger.LogInformation("New file detected: {Path}", Path.GetFileName(e.FullPath));
        FileDetected?.Invoke(this, e.FullPath);
    }

    public void Start() => _watcher.EnableRaisingEvents = true;

    public void Stop() => _watcher.EnableRaisingEvents = false;

    public void Dispose()
    {
        Stop();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher.Dispose();
        }
    }
}