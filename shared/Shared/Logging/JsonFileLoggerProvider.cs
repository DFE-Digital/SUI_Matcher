using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Shared.Logging;

public class JsonFileLoggerProvider(string filePath) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new JsonFileLogger(filePath, categoryName);

    public void Dispose() { }

    private class JsonFileLogger(string filePath, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logEntry = new
            {
                TimeStamp = DateTime.Now,
                LogLevel = logLevel.ToString(),
                Category = categoryName,
                Message = formatter(state, exception),
                Exception = exception?.ToString()
            };

            var logJson = JsonSerializer.Serialize(logEntry);

            var finalFilePath = filePath;
            var timeStamp = $"{logEntry.TimeStamp:yyyy-MM-dd}";
            
            if (finalFilePath.EndsWith(".json"))
            {
                finalFilePath = finalFilePath.Replace(".json", $"-{timeStamp}.log");
            } 
            else if (finalFilePath.EndsWith(".log"))
            {
                finalFilePath = finalFilePath.Replace(".log", $"-{timeStamp}.log");
            }
            else
            {
                finalFilePath += $"-{timeStamp}.log";
            }

            File.AppendAllText(finalFilePath, logJson + Environment.NewLine);
        }
    }
}