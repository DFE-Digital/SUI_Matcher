using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Shared.Logging;

namespace Unit.Tests.Logging;

public class LogConsoleFormatterTests
{
    private readonly LogConsoleFormatter _formatter = new();
    private readonly StringWriter _writer = new();

    [Fact]
    public void Write_WithSearchIdAndAlgorithmVersion_WritesFullFormat()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            ("AlgorithmVersion", "1")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] [Algorithm=v1] [SearchId=123] Test Message");
    }

    [Fact]
    public void Write_WithSearchIdAndAlgorithmVersionAndStrategyName_WritesFullFormat()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            ("AlgorithmVersion", "1"),
            (SharedConstants.SearchStrategy.LogName, "strat1")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine(
            "[Information] [Algorithm=v1] [SearchStrategy=strat1] [SearchId=123] Test Message"
        );
    }

    [Fact]
    public void Write_WithOnlySearchId_WritesPartialFormat()
    {
        using var activity = StartActivityWithBaggage(("SearchId", "456"));

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] [SearchId=456] Test Message");
    }

    [Fact]
    public void Write_WithOnlyReconciliationId_WritesPartialFormat()
    {
        using var activity = StartActivityWithBaggage(("ReconciliationId", "456"));

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] [ReconciliationId=456] Test Message");
    }

    [Fact]
    public void Write_WithSearchIdAndReconciliationId_PrefersSearchId()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            ("ReconciliationId", "456")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] [SearchId=123] Test Message");
    }

    [Fact]
    public void Write_WithReconciliationIdAndAlgorithmVersionAndStrategyName_WritesAllFields()
    {
        using var activity = StartActivityWithBaggage(
            ("ReconciliationId", "456"),
            ("AlgorithmVersion", "1"),
            (SharedConstants.SearchStrategy.LogName, "strat1")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine(
            "[Information] [Algorithm=v1] [SearchStrategy=strat1] [ReconciliationId=456] Test Message"
        );
    }

    [Fact]
    public void Write_WithSearchIdAndStrategyNameOnly_WritesStrategyName()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            (SharedConstants.SearchStrategy.LogName, "strat1")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] [SearchStrategy=strat1] [SearchId=123] Test Message");
    }

    [Fact]
    public void Write_WithQueryName_WritesQueryNameToOutput()
    {
        using var activity = StartActivityWithBaggage(
            (SharedConstants.SearchQuery.LogName, "NonFuzzyGFD")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] [QueryName=NonFuzzyGFD] Test Message");
    }

    [Fact]
    public void Write_WithAllBaggage_WritesQueryNameBetweenStrategyAndSearchId()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            ("AlgorithmVersion", "1"),
            (SharedConstants.SearchStrategy.LogName, "strat1"),
            (SharedConstants.SearchQuery.LogName, "NonFuzzyGFD")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine(
            "[Information] [Algorithm=v1] [SearchStrategy=strat1] [QueryName=NonFuzzyGFD] [SearchId=123] Test Message"
        );
    }

    [Fact]
    public void Write_WithoutQueryName_OmitsQueryNameFromOutput()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            ("AlgorithmVersion", "1"),
            (SharedConstants.SearchStrategy.LogName, "strat1")
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        Assert.DoesNotContain(SharedConstants.SearchQuery.LogName, _writer.ToString());
        AssertFormattedLine(
            "[Information] [Algorithm=v1] [SearchStrategy=strat1] [SearchId=123] Test Message"
        );
    }

    [Fact]
    public void Write_WithEmptyQueryName_OmitsQueryNameFromOutput()
    {
        using var activity = StartActivityWithBaggage(
            ("SearchId", "123"),
            (SharedConstants.SearchQuery.LogName, string.Empty)
        );

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        Assert.DoesNotContain(SharedConstants.SearchQuery.LogName, _writer.ToString());
        AssertFormattedLine("[Information] [SearchId=123] Test Message");
    }

    [Fact]
    public void Write_WithoutBaggage_WritesBasicFormat()
    {
        using var activity = StartActivityWithBaggage();

        var logEntry = CreateLogEntry(LogLevel.Information);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        AssertFormattedLine("[Information] Test Message");
    }

    [Fact]
    public void Write_NullMessage_DoesNothing()
    {
        var logEntry = CreateLogEntry(LogLevel.Information, true);

        _formatter.Write(in logEntry, scopeProvider: null, _writer);

        var output = _writer.ToString();
        Assert.Equal(string.Empty, output);
    }

    /// <summary>
    /// Asserts the whole written line, ignoring only the leading timestamp (which is
    /// non-deterministic), so that field content, field order and spacing are all pinned.
    /// </summary>
    private void AssertFormattedLine(string expectedAfterTimestamp)
    {
        var output = _writer.ToString();
        var separatorIndex = output.IndexOf(" [", StringComparison.Ordinal);

        Assert.True(separatorIndex > 0, $"Expected a timestamp prefix but got '{output}'.");

        var timestamp = output[..separatorIndex];
        Assert.True(
            DateTime.TryParse(timestamp, out _),
            $"Expected a parsable timestamp prefix but got '{timestamp}'."
        );

        Assert.Equal(expectedAfterTimestamp + Environment.NewLine, output[(separatorIndex + 1)..]);
    }

    private static Activity StartActivityWithBaggage(params (string Key, string Value)[] baggage)
    {
        var activity = new Activity("TestActivity");

        foreach ((string key, string value) in baggage)
        {
            activity.AddBaggage(key, value);
        }

        activity.Start();
        Activity.Current = activity;

        return activity;
    }

    private static LogEntry<string> CreateLogEntry(LogLevel level, bool nullState = false)
    {
        return new LogEntry<string>(
            level,
            "Test Category",
            new EventId(0),
            "Test Message",
            null,
            (state, ex) => (nullState ? null : state.ToString())!
        );
    }
}
