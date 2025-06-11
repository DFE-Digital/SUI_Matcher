using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

using Shared.Logging;

namespace Unit.Tests.Util;

public class TestConsoleFormatterOptions : ConsoleFormatterOptions
{
    public List<string> TestLogMessages { get; set; } = [];
}

public class TestLogConsoleFormatter(IOptionsMonitor<TestConsoleFormatterOptions> options) : LogConsoleFormatter
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        base.Write(logEntry, scopeProvider, textWriter);

        foreach (string logLine in (textWriter.ToString()?.Split("\n") ?? []))
        {
            options.CurrentValue.TestLogMessages.Add(logLine);
        }
    }
}