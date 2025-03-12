using Microsoft.Extensions.Logging;

namespace SUI.Test.Integration;

public class TestContextLoggerProvider : ILoggerProvider
{
    private readonly TestContext _testContext;

    public TestContextLoggerProvider(TestContext testContext)
    {
        _testContext = testContext;
    }

    public ILogger CreateLogger(string categoryName) => new TestContextLogger(_testContext, categoryName);

    public void Dispose() { }

    private class TestContextLogger : ILogger
    {
        private readonly TestContext _testContext;
        private readonly string _categoryName;

        public TestContextLogger(TestContext testContext, string categoryName)
        {
            _testContext = testContext;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _testContext.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
        }
    }
}
