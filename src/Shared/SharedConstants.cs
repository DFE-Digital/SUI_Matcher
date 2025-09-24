namespace Shared;

public static class SharedConstants
{
    public const string LogFormatter = "custom-formatter";

    public static class AuditLog
    {
        public const string AzStorageTableName = "AuditLogs";
    }

    public static class SearchStrategy
    {
        public const string LogName = "SearchStrategy";
        public static class Strategies
        {
            public const string Strategy1 = "strategy1";
        }
    }
}