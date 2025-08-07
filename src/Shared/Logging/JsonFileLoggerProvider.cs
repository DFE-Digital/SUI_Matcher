using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Shared.Logging;

[ExcludeFromCodeCoverage(Justification = "This is a logging provider purely for writing logs.")]
public partial class JsonFileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly JsonFileLogger _logger = new(filePath, nameof(JsonFileLoggerProvider));

    public virtual ILogger CreateLogger(string categoryName) => _logger;

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.Close();
        }
    }

    protected partial class JsonFileLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        protected StreamWriter _logFileWriter;
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

            var timeStamp = DateTime.Now;

            // to timestamp historic dbs searches in their date specific log files
            var responseFileLastModified = Activity.Current?.GetBaggageItem("responseFileLastModified");
            if (responseFileLastModified is not null &&
                DateTime.TryParse(responseFileLastModified, CultureInfo.InvariantCulture, out DateTime responseFileLastModifiedDate))
            {
                timeStamp = responseFileLastModifiedDate.Date + timeStamp.TimeOfDay;
            }

            var logEntry = new Dictionary<string, object?>
            {
                { "TimeStamp", timeStamp },
                { "LogLevel", logLevel.ToString() },
                { "Category", _categoryName },
                { "Message", message },
                { "Exception", exception?.ToString() }
            };

            if (state is IEnumerable<KeyValuePair<string, object?>> formattedLogValues)
            {
                foreach (var item in formattedLogValues
                             .Where(item => WordOnlyRegex().IsMatch(item.Key)))
                {
                    logEntry[item.Key] = item.Value;
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

        [GeneratedRegex(@"^\w+$")]
        private static partial Regex WordOnlyRegex();
    }
}