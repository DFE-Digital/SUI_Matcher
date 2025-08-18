using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shared.Logging;

namespace Unit.Tests.Logging;

public class LogConsoleFormatterTests
{
    private readonly LogConsoleFormatter _formatter = new();
    private readonly StringWriter _writer = new();

    [Fact]
    public void Write_WithSearchIdAndAlgorithmVersion_WritesFullFormat()
    {
        using var activity = new Activity("TestActivity");

        activity.AddBaggage("SearchId", "123");
        activity.AddBaggage("AlgorithmVersion", "1");
        activity.Start();
        Activity.Current = activity;

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        var output = _writer.ToString();
        Assert.Contains("[Information] [Algorithm=v1] [SearchId=123] Test Message", output);
    }

    [Fact]
    public void Write_WithOnlySearchId_WritesPartialFormat()
    {
        using var activity = new Activity("TestActivity");

        activity.AddBaggage("SearchId", "456");
        activity.Start();
        Activity.Current = activity;

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        var output = _writer.ToString();
        Assert.Contains("[Information] [SearchId=456] Test Message", output);
    }

    [Fact]
    public void Write_WithOnlyReconciliationId_WritesPartialFormat()
    {
        using var activity = new Activity("TestActivity");

        activity.AddBaggage("ReconciliationId", "456");
        activity.Start();
        Activity.Current = activity;

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        var output = _writer.ToString();
        Assert.Contains("[Information] [ReconciliationId=456] Test Message", output);
    }

    [Fact]
    public void Write_WithoutBaggage_WritesBasicFormat()
    {
        using var activity = new Activity("TestActivity");

        activity.Start();
        Activity.Current = activity;

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        var output = _writer.ToString();
        Assert.Contains("[Information] Test Message", output);
    }

    [Fact]
    public void Write_NullMessage_DoesNothing()
    {
        var logEntry = CreateLogEntry(LogLevel.Information, true);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        var output = _writer.ToString();
        Assert.Equal(string.Empty, output);
    }

    private static LogEntry<string> CreateLogEntry(LogLevel level, bool nullState = false)
    {
        return new LogEntry<string>(
            level,
            "Test Category",
            new EventId(0),
            "Test Message",
            null,
            (state, ex) => (nullState ? null : state.ToString())!);
    }
}