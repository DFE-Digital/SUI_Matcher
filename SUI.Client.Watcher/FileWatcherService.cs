using Microsoft.Extensions.Logging;

namespace SUI.Client.Watcher;

public class FileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly AppConfig _config;
    private readonly ILogger _logger;
    public event EventHandler<string>? FileDetected;

    public FileWatcherService(AppConfig config, ILoggerFactory loggerFactory)
    {
        _config = config;
        _logger = (ILogger) loggerFactory.CreateLogger<FileWatcherService>();
        Directory.CreateDirectory(_config.IncomingDirectory);
        _watcher = new FileSystemWatcher(_config.IncomingDirectory, _config.FileFilter)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        _watcher.Created += OnCreated;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        _logger.Log($"New file detected: {Path.GetFileName(e.FullPath)}");
        FileDetected?.Invoke(this, e.FullPath);
    }

    public void Start() => _watcher.EnableRaisingEvents = true;

    public void Stop() => _watcher.EnableRaisingEvents = false;

    public void Dispose()
    {
        Stop();
        _watcher.Dispose();
    }
}
