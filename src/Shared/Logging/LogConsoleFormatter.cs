
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Shared.Logging;

public class LogConsoleFormatter() : ConsoleFormatter("custom-formatter")
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
        var algorithmVersion = Activity.Current?.GetBaggageItem("AlgorithmVersion");

        if (searchId is not null && algorithmVersion is not null)
        {
            textWriter.Write($"[{logEntry.LogLevel}] [Algorithm=v{algorithmVersion}] [SearchId={searchId}] ");
        }
        else if (searchId is not null)
        {
            textWriter.Write($"[{logEntry.LogLevel}] [SearchId={searchId}] ");
        }
        else
        {
            textWriter.Write($"[{logEntry.LogLevel}] ");
        }

        textWriter.WriteLine(message);
    }

}