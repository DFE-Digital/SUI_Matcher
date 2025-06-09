using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SUI.Client.Core.Watcher;

[ExcludeFromCodeCoverage(Justification = "Uses real file system events, not mockable and permissions dependent")]
public class CsvFileMonitor
{
    private readonly CsvFileWatcherService _fileWatcherService;
    private readonly CsvWatcherConfig _config;
    private readonly ILogger<CsvFileMonitor> _logger;
    private readonly ICsvFileProcessor _fileProcessor;
    private readonly ConcurrentQueue<string> _fileQueue = new();
    private int _processedCount;
    private int _errorCount;
    public int ProcessedCount => _processedCount;
    public int ErrorCount => _errorCount;
    public event EventHandler<FileProcessedEnvelope>? Processed;

    public FileProcessedEnvelope? LastOperation { get; private set; }

    public FileProcessedEnvelope GetLastOperation() => LastOperation ?? throw new InvalidOperationException("LastOperation is null");

    public ProcessCsvFileResult LastResult() => GetLastOperation().AssertSuccess();

    public CsvFileMonitor(CsvFileWatcherService fileWatcherService, IOptions<CsvWatcherConfig> config, ILogger<CsvFileMonitor> logger, ICsvFileProcessor fileProcessor)
    {
        _fileWatcherService = fileWatcherService;
        _config = config.Value;
        _logger = logger;
        _fileProcessor = fileProcessor;
        Directory.CreateDirectory(_config.ProcessedDirectory);
        _fileWatcherService.FileDetected += (_, filePath) => _fileQueue.Enqueue(filePath);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _fileWatcherService.Start();
        _logger.LogInformation("Started watching {Directory}", _config.IncomingDirectory);
        try
        {
            await ProcessInternalAsync(cancellationToken);
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(exception, "Cancelled. Stopping...");
        }
        _fileWatcherService.Stop();
        _logger.LogInformation("Stopped watching {Directory}", _config.IncomingDirectory);
    }

    private async Task ProcessInternalAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_fileQueue.TryDequeue(out var filePath) && File.Exists(filePath))
            {
                _logger.LogInformation("Discovered file: {FileName}", Path.GetFileName(filePath));
                try
                {
                    var processCsvFileResult = await ProcessFileAsync(filePath);
                    await RetryAsync(() =>
                    {
                        string destPath = Path.Combine(processCsvFileResult.OutputDirectory, Path.GetFileName(filePath));
                        File.Move(filePath, destPath);
                        _logger.LogInformation("File moved to Processed directory: {DestPath}", destPath);
                        Interlocked.Increment(ref _processedCount);
                        return Task.CompletedTask;
                    }, _config.RetryCount, _config.RetryDelayMs);

                    LastOperation = new FileProcessedEnvelope(filePath, result: processCsvFileResult);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Error processing file {FilePath}: {Message}", filePath, ex.Message);
                    Interlocked.Increment(ref _errorCount);
                    LastOperation = new FileProcessedEnvelope(filePath, exception: ex);
                }

                _logger.LogInformation("Finished processing file: {fileName}", Path.GetFileName(filePath));
                Processed?.Invoke(this, LastOperation);
            }
            await Task.Delay(_config.ProcessingDelayMs, cancellationToken);
        }
    }

    private async Task<ProcessCsvFileResult> ProcessFileAsync(string filePath)
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
                _logger.LogInformation(ex, "Retry attempt {Attempts} failed: {Message}. Retrying in {DelayMs}ms.",
                    attempts, ex.Message, delayMs);
                await Task.Delay(delayMs);
            }
        }
    }
}