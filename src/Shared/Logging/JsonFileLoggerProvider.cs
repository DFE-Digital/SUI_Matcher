using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Shared.Logging;

public class JsonFileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly JsonFileLogger _logger = new(filePath, nameof(JsonFileLoggerProvider));

    public ILogger CreateLogger(string categoryName) => _logger;

    public void Dispose()
    {
        _logger.Close();
        GC.SuppressFinalize(this);
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
            string message = formatter(state, exception);

            var searchId = Activity.Current?.GetBaggageItem("SearchId");

            if (searchId is not null)
            {
                message = $"[{logLevel}] [SearchId={searchId}] " + message;
            }
            else
            {
                message = $"[{logLevel}] " + message;
            }

            var logEntry = new Dictionary<string, object?>
            {
                { "TimeStamp", DateTime.Now },
                { "LogLevel", logLevel.ToString() },
                { "Category", _categoryName },
                { "Message", message },
                { "Exception", exception?.ToString() }
            };

            if (state is IEnumerable<KeyValuePair<string, object?>> formattedLogValues)
            {
                foreach (var item in formattedLogValues)
                {
                    if (Regex.IsMatch(item.Key, @"^\w+$"))
                    {
                        logEntry[item.Key] = item.Value;
                    }
                }
            }

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