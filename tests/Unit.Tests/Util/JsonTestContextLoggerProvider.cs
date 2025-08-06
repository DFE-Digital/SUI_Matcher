using Microsoft.Extensions.Logging;

using Shared.Logging;

using Xunit.Abstractions;

namespace Unit.Tests.Util;

public class JsonTestContextLoggerProvider(
    ITestOutputHelper testContext,
    List<string> logMessages,
    string filePath = "dbs-response-logger-logs.json")
    : JsonFileLoggerProvider(filePath)
{
    private readonly string _filePath = filePath;

    public override ILogger CreateLogger(string categoryName) => new JsonTestContextLogger(testContext, categoryName, _filePath, logMessages);

    private class TestStreamWriter : StreamWriter
    {
        private readonly List<string> _logMessages;

        public TestStreamWriter(string filePath, List<string> logMessages) : base(filePath, append: true)
        {
            base.AutoFlush = true;
            _logMessages = logMessages;
        }

        public override void WriteLine(string? value)
        {
            base.WriteLine(value);

            _logMessages.Add(value!);
        }
    }

    private class JsonTestContextLogger : JsonFileLogger, ILogger
    {
        private readonly ITestOutputHelper _testContext;
        private readonly string _categoryName;
        private readonly string _filePath;

        public JsonTestContextLogger(ITestOutputHelper testContext, string categoryName, string filePath, List<string> logMessages) : base(filePath, "")
        {
            _testContext = testContext;
            _categoryName = categoryName;
            _filePath = filePath;
            _logFileWriter = new TestStreamWriter(filePath, logMessages);
        }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public new bool IsEnabled(LogLevel logLevel) => true;
    }
}