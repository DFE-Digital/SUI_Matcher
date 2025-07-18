namespace Shared.Logging;

public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry);
}

public class AuditLogEntry(AuditLogEntry.AuditLogAction action, Dictionary<string, string>? metadata)
{
    public AuditLogAction Action { get; } = action;
    public Dictionary<string, string> Metadata { get; } = metadata ?? new Dictionary<string, string>();

    public enum AuditLogAction
    {
        Match,
        Demographic,
    }
}