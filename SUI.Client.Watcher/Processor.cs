using System.Collections.Concurrent;

namespace SUI.Client.Watcher;

public class Processor
{
    private readonly FileWatcherService _fileWatcherService;
    private readonly AppConfig _config;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<string> _fileQueue = new();
    private int _processedCount = 0;
    private int _errorCount = 0;

    public Processor(FileWatcherService fileWatcherService, AppConfig config, ILogger logger)
    {
        _fileWatcherService = fileWatcherService;
        _config = config;
        _logger = logger;
        Directory.CreateDirectory(_config.ProcessedDirectory);
        Directory.CreateDirectory(_config.LogDirectory);
        _fileWatcherService.FileDetected += (s, filePath) => _fileQueue.Enqueue(filePath);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _fileWatcherService.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_fileQueue.TryDequeue(out string filePath))
            {
                try
                {
                    await ProcessFileAsync(filePath);
                    // Retry moving the file in case of transient errors.
                    await RetryAsync(async () =>
                    {
                        string destPath = Path.Combine(_config.ProcessedDirectory, Path.GetFileName(filePath));
                        File.Move(filePath, destPath);
                        _logger.Log($"File moved to Processed directory: {destPath}");
                    }, _config.RetryCount, _config.RetryDelayMs);

                    Interlocked.Increment(ref _processedCount);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error processing file {filePath}: {ex.Message}");
                    Interlocked.Increment(ref _errorCount);
                }
            }
            await Task.Delay(_config.ProcessingDelayMs, cancellationToken);
        }
        _fileWatcherService.Stop();
    }

    private async Task ProcessFileAsync(string filePath)
    {
        // For example, asynchronously read and process CSV content here.
        await Task.CompletedTask;
    }

    public void PrintStats(TextWriter output)
    {
        output.WriteLine($"Processed Count: {_processedCount}, Error Count: {_errorCount}");
    }

    private async Task RetryAsync(Func<Task> action, int retryCount, int delayMs)
    {
        int attempts = 0;
        while (attempts < retryCount)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts >= retryCount)
                    throw;
                _logger.Log($"Retry attempt {attempts} failed: {ex.Message}. Retrying in {delayMs}ms.");
                await Task.Delay(delayMs);
            }
        }
    }
}
