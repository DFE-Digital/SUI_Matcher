using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace Shared.Logging;

public class JsonFileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly JsonFileLogger _logger = new(filePath, nameof(JsonFileLoggerProvider));

    public ILogger CreateLogger(string categoryName) => _logger;

    public void Dispose()
    {
        _logger.Close();
    }

    private class JsonFileLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        private readonly StreamWriter _logFileWriter;
        private readonly string _categoryName;

        public JsonFileLogger(string logFilePath, string categoryName)
        {
            var timeStamp = $"{DateTime.Now:yyyy-MM-dd}";

            if (logFilePath.EndsWith(".json"))
            {
                logFilePath = logFilePath.Replace(".json", $"-{timeStamp}.log");
            }
            else if (logFilePath.EndsWith(".log"))
            {
                logFilePath = logFilePath.Replace(".log", $"-{timeStamp}.log");
            }
            else
            {
                logFilePath += $"-{timeStamp}.log";
            }

            _logFileWriter = new StreamWriter(logFilePath, append: true);
            _logFileWriter.AutoFlush = true;
            _categoryName = categoryName;

        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logEntry = new
            {
                TimeStamp = DateTime.Now,
                LogLevel = logLevel.ToString(),
                Category = _categoryName,
                Message = formatter(state, exception),
                Exception = exception?.ToString()
            };

            var logJson = JsonSerializer.Serialize(logEntry);

            _logFileWriter.WriteLine(logJson);
        }

        public void Close()
        {
            _logFileWriter.Close();
            _logFileWriter.Dispose();
        }
    }
}