using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

namespace Unit.Tests.Util;

public class TestContextLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testContext;
    private readonly List<string> _logMessages;

    public TestContextLoggerProvider(ITestOutputHelper testContext, List<string>? logMessages = null)
    {
        _testContext = testContext;
        _logMessages = logMessages ?? new();
    }

    public ILogger CreateLogger(string categoryName) => new TestContextLogger(_testContext, categoryName, _logMessages);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private class TestContextLogger : ILogger
    {
        private readonly ITestOutputHelper _testContext;
        private readonly string _categoryName;
        private readonly List<string> _logMessages;

        public TestContextLogger(ITestOutputHelper testContext, string categoryName, List<string>? logMessages = null)
        {
            _testContext = testContext;
            _categoryName = categoryName;
            _logMessages = logMessages ?? new();
        }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _testContext.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");

            _logMessages.Add($"{formatter(state, exception)}");
        }
    }
}