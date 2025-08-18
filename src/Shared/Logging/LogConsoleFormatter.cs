
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Shared.Logging;

public class LogConsoleFormatter() : ConsoleFormatter(Shared.Constants.LogFormatter)
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string? message =
            logEntry.Formatter?.Invoke(
                logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        var searchId = Activity.Current?.GetBaggageItem("SearchId");
        var reconcilationId = Activity.Current?.GetBaggageItem("ReconciliationId");
        var algorithmVersion = Activity.Current?.GetBaggageItem("AlgorithmVersion");

        if (searchId is not null && algorithmVersion is not null)
        {
            textWriter.Write($"{DateTime.UtcNow} [{logEntry.LogLevel}] [Algorithm=v{algorithmVersion}] [SearchId={searchId}] ");
        }
        else if (searchId is not null)
        {
            textWriter.Write($"{DateTime.UtcNow} [{logEntry.LogLevel}] [SearchId={searchId}] ");
        }
        else if (reconcilationId is not null)
        {
            textWriter.Write($"{DateTime.UtcNow} [{logEntry.LogLevel}] [ReconciliationId={reconcilationId}] ");
        }
        else
        {
            textWriter.Write($"{DateTime.UtcNow} [{logEntry.LogLevel}] ");
        }

        textWriter.WriteLine(message);
    }

}