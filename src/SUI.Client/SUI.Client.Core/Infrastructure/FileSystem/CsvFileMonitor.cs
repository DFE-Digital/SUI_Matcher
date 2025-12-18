using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Util;

namespace SUI.Client.Core.Infrastructure.FileSystem;

[ExcludeFromCodeCoverage(Justification = "Uses real file system events, not mockable and permissions dependent")]
public class CsvFileMonitor : IDisposable
{
    private readonly CsvWatcherConfig _config;
    private readonly ILogger<CsvFileMonitor> _logger;
    private readonly ICsvFileProcessor _fileProcessor;
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentQueue<string> _fileQueue = new();
    private int _processedCount;
    private int _errorCount;
    public int ProcessedCount => _processedCount;
    public int ErrorCount => _errorCount;
    public event EventHandler<FileProcessedEnvelope>? Processed;

    public FileProcessedEnvelope? LastOperation { get; private set; }

    public FileProcessedEnvelope GetLastOperation() => LastOperation ?? throw new InvalidOperationException("LastOperation is null");

    public ProcessCsvFileResult LastResult() => GetLastOperation().AssertSuccess();

    public CsvFileMonitor(IOptions<CsvWatcherConfig> config, ILogger<CsvFileMonitor> logger, ICsvFileProcessor fileProcessor)
    {
        _config = config.Value;
        _logger = logger;
        _fileProcessor = fileProcessor;

        Directory.CreateDirectory(_config.IncomingDirectory);
        Directory.CreateDirectory(_config.ProcessedDirectory);
        _watcher = new FileSystemWatcher(_config.IncomingDirectory, "*.csv")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        };
        _watcher.Created += (_, e) => _fileQueue.Enqueue(e.FullPath);
    }

    public void Dispose()
    {
        Dispose(true); // Disposer pattern
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("Started watching {Directory}", _config.IncomingDirectory);
        try
        {
            await ProcessInternalAsync(cancellationToken);
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(exception, "Cancelled. Stopping...");
        }
        _watcher.EnableRaisingEvents = false;
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
                    await RetryUtil.RetryAsync(() =>
                    {
                        string destPath = Path.Combine(processCsvFileResult.OutputDirectory, Path.GetFileName(filePath));
                        File.Move(filePath, destPath);
                        _logger.LogInformation("File moved to Processed directory: {DestPath}", destPath);
                        Interlocked.Increment(ref _processedCount);
                        return Task.CompletedTask;
                    }, _config.RetryCount, _config.RetryDelayMs, _logger);

                    LastOperation = new FileProcessedEnvelope(filePath, result: processCsvFileResult);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Error processing file {FilePath}: {Message}", filePath, ex.Message);
                    Interlocked.Increment(ref _errorCount);
                    LastOperation = new FileProcessedEnvelope(filePath, exception: ex);
                }

                _logger.LogInformation("Finished processing file: {FileName}", Path.GetFileName(filePath));
                Processed?.Invoke(this, LastOperation);
            }
            await Task.Delay(_config.ProcessingDelayMs, cancellationToken);
        }
    }

    private async Task<ProcessCsvFileResult> ProcessFileAsync(string filePath)
    {
        var inputData = await ReadCsvAsync(filePath);

        _logger.LogInformation("Beginning to process {TotalRecords} records from file: {FilePath}", inputData.Rows.Count,
            filePath);
        return await _fileProcessor.ProcessCsvFileAsync(inputData, _config.ProcessedDirectory);
    }

    public void PrintStats(TextWriter output)
    {
        output.WriteLine($"Processed Count: {_processedCount}, Error Count: {_errorCount}");
    }


    public static async Task<DataTable> ReadCsvAsync(string filePath)
    {
        var dt = new DataTable();

        if (!await IsFileReadyAsync(filePath))
        {
            throw new IOException($"File {filePath} is not ready for reading.");
        }

        using var reader = new StreamReader(filePath);

        CsvConfiguration readerConfiguration = new(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };
        using var csv = new CsvReader(reader, readerConfiguration);

        using var dr = new CsvDataReader(csv);
        dt.Load(dr);
        dt.TableName = Path.GetFileNameWithoutExtension(filePath);

        foreach (DataColumn col in dt.Columns)
        {
            col.ReadOnly = false; // Ensure table is editable
        }

        return dt;
    }

    private static async Task<bool> IsFileReadyAsync(string filePath, int maxAttempts = 5, int delayMs = 1000)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                await Task.Delay(delayMs);
            }
        }

        return false;
    }
}