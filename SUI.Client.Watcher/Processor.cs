using Microsoft.Extensions.Logging;
using SUI.Client.Core;
using System.Collections.Concurrent;

namespace SUI.Client.Watcher;

public class CsvFileMonitor
{
    private readonly CsvFileWatcherService _fileWatcherService;
    private readonly CsvWatcherConfig _config;
    private readonly ILogger<CsvFileMonitor> _logger;
    private readonly ICsvFileProcessor _fileProcessor;
    private readonly ConcurrentQueue<string> _fileQueue = new();
    private int _processedCount = 0;
    private int _errorCount = 0;
    public int ProcessedCount => _processedCount;
    public int ErrorCount => _errorCount;
    public event EventHandler<FileProcessedResult>? Processed;
    public FileProcessedResult? LastResult { get; private set; }

    public CsvFileMonitor(CsvFileWatcherService fileWatcherService, CsvWatcherConfig config, ILogger<CsvFileMonitor> logger, ICsvFileProcessor fileProcessor)
    {
        _fileWatcherService = fileWatcherService;
        _config = config;
        _logger = logger;
        _fileProcessor = fileProcessor;
        Directory.CreateDirectory(_config.ProcessedDirectory);
        Directory.CreateDirectory(_config.LogDirectory);
        _fileWatcherService.FileDetected += (s, filePath) => _fileQueue.Enqueue(filePath);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _fileWatcherService.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_fileQueue.TryDequeue(out var filePath))
            {
                try
                {
                    var of = await ProcessFileAsync(filePath);
                    await RetryAsync(async () =>
                    {
                        string destPath = Path.Combine(_config.ProcessedDirectory, Path.GetFileName(filePath));
                        File.Move(filePath, destPath);
                        _logger.LogInformation($"File moved to Processed directory: {destPath}");
                    }, _config.RetryCount, _config.RetryDelayMs);

                    Interlocked.Increment(ref _processedCount);
                    LastResult = new FileProcessedResult(filePath, outputFile: of);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error processing file {filePath}: {ex.Message}");
                    Interlocked.Increment(ref _errorCount);
                    LastResult = new FileProcessedResult(filePath, exception: ex);
                }
                Processed?.Invoke(this, LastResult);
            }
            await Task.Delay(_config.ProcessingDelayMs, cancellationToken);
        }
        _fileWatcherService.Stop();
    }

    private async Task<string> ProcessFileAsync(string filePath) 
        => await _fileProcessor.ProcessCsvFileAsync(filePath, _config.ProcessedDirectory);

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
                _logger.LogInformation($"Retry attempt {attempts} failed: {ex.Message}. Retrying in {delayMs}ms.");
                await Task.Delay(delayMs);
            }
        }
    }
}
