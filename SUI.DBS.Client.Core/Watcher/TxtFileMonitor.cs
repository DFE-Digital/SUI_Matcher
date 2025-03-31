﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace SUI.DBS.Client.Core.Watcher;

public class TxtFileMonitor
{
    private readonly TxtFileWatcherService _fileWatcherService;
    private readonly TxtWatcherConfig _config;
    private readonly ILogger<TxtFileMonitor> _logger;
    private readonly ITxtFileProcessor _fileProcessor;
    private readonly ConcurrentQueue<string> _fileQueue = new();
    private int _processedCount = 0;
    private int _errorCount = 0;
    public int ProcessedCount => _processedCount;
    public int ErrorCount => _errorCount;
    public event EventHandler<FileProcessedEnvelope>? Processed;

    public FileProcessedEnvelope? LastOperation { get; private set; }

    public FileProcessedEnvelope GetLastOperation() => LastOperation ?? throw new Exception("LastResult is null");

    public TxtFileMonitor(TxtFileWatcherService fileWatcherService, IOptions<TxtWatcherConfig> config, ILogger<TxtFileMonitor> logger, ITxtFileProcessor fileProcessor)
    {
        _fileWatcherService = fileWatcherService;
        _config = config.Value;
        _logger = logger;
        _fileProcessor = fileProcessor;
        Directory.CreateDirectory(_config.ProcessedDirectory);
        _fileWatcherService.FileDetected += (s, filePath) => _fileQueue.Enqueue(filePath);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _fileWatcherService.Start();
        _logger.LogInformation("Started watching {directory}", _config.IncomingDirectory);
        try
        {
            await ProcessInternalAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Cancelled. Stopping...");
        }
        _fileWatcherService.Stop();
        _logger.LogInformation("Stopped watching {directory}", _config.IncomingDirectory);
    }

    private async Task ProcessInternalAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_fileQueue.TryDequeue(out var filePath))
            {
                _logger.LogInformation("Discovered file: {fileName}", Path.GetFileName(filePath));
                try
                {
                    await ProcessFileAsync(filePath);
                    await RetryAsync(() =>
                    {
                        string destPath = Path.Combine(_config.ProcessedDirectory, Path.GetFileName(filePath));
                        File.Move(filePath, destPath);
                        _logger.LogInformation("File moved to Processed directory: {destPath}", destPath);
                        Interlocked.Increment(ref _processedCount);
                        return Task.CompletedTask;
                    }, _config.RetryCount, _config.RetryDelayMs);

                    LastOperation = new FileProcessedEnvelope(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error processing file {filePath}: {ex.Message}");
                    Interlocked.Increment(ref _errorCount);
                    LastOperation = new FileProcessedEnvelope(filePath, exception: ex);
                }
                Processed?.Invoke(this, LastOperation);
            }
            await Task.Delay(_config.ProcessingDelayMs, cancellationToken);
        }
    }
    
    public void PrintStats(TextWriter output)
    {
        output.WriteLine($"Processed Count: {_processedCount}, Error Count: {_errorCount}");
    }

    private async Task ProcessFileAsync(string filePath) 
        => await _fileProcessor.ProcessFileAsync(filePath);

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
