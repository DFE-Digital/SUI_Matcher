﻿using Microsoft.Extensions.Logging;

namespace SUI.Test.Integration;

public class TestContextLoggerProvider : ILoggerProvider
{
    private readonly TestContext _testContext;
    private readonly List<string> _logMessages;

    public TestContextLoggerProvider(TestContext testContext, List<string>? logMessages = null)
    {
        _testContext = testContext;
        _logMessages = logMessages ?? new();
    }

    public ILogger CreateLogger(string categoryName) => new TestContextLogger(_testContext, categoryName, _logMessages);

    public void Dispose() { }

    private class TestContextLogger : ILogger
    {
        private readonly TestContext _testContext;
        private readonly string _categoryName;
        private readonly List<string> _logMessages;

        public TestContextLogger(TestContext testContext, string categoryName, List<string>? logMessages = null)
        {
            _testContext = testContext;
            _categoryName = categoryName;
            _logMessages = logMessages ?? new();
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _testContext.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            
            _logMessages.Add($"{formatter(state, exception)}");
        }
    }
}
