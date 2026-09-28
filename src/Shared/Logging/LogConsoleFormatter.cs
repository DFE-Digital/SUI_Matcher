using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Shared.Logging;

public class LogConsoleFormatter() : ConsoleFormatter(Shared.SharedConstants.LogFormatter)
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        var searchId = Activity.Current?.GetBaggageItem("SearchId");
        var reconcilationId = Activity.Current?.GetBaggageItem("ReconciliationId");
        var algorithmVersion = Activity.Current?.GetBaggageItem("AlgorithmVersion");
        var strategy = Activity.Current?.GetBaggageItem(SharedConstants.SearchStrategy.LogName);
        var queryName = Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName);

        textWriter.Write($"{DateTime.UtcNow} [{logEntry.LogLevel}] ");

        if (algorithmVersion is not null)
        {
            textWriter.Write($"[Algorithm=v{algorithmVersion}] ");
        }

        if (!string.IsNullOrEmpty(strategy))
        {
            textWriter.Write($"[{SharedConstants.SearchStrategy.LogName}={strategy}] ");
        }

        if (!string.IsNullOrEmpty(queryName))
        {
            textWriter.Write($"[{SharedConstants.SearchQuery.LogName}={queryName}] ");
        }

        if (searchId is not null)
        {
            textWriter.Write($"[SearchId={searchId}] ");
        }
        else if (reconcilationId is not null)
        {
            textWriter.Write($"[ReconciliationId={reconcilationId}] ");
        }

        textWriter.WriteLine(message);
    }
}
