using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SUI.DBS.Response.Logger.Core.Watcher;

/// <summary>
/// Wrapper for FileSystemWatcher
/// </summary>
public sealed class TxtFileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    
    private readonly ILogger _logger;
    public event EventHandler<string>? FileDetected;

    public int Count { get; private set; }

    public TxtFileWatcherService(IOptions<TxtWatcherConfig> config, ILoggerFactory loggerFactory)
    {
        TxtWatcherConfig config1 = config.Value;
        _logger = loggerFactory.CreateLogger<TxtFileWatcherService>();
        Directory.CreateDirectory(config1.IncomingDirectory);
        _watcher = new FileSystemWatcher(config1.IncomingDirectory, "*.txt")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        _watcher.Created += OnCreated;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        Count++;
        _logger.LogInformation("New file detected: {Filename}", Path.GetFileName(e.FullPath));
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