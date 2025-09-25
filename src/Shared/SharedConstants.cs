namespace Shared;

public static class SharedConstants
{
    public const string LogFormatter = "custom-formatter";

    public static class AuditLog
    {
        public const string AzStorageTableName = "AuditLogs";
    }

    public static class SearchQuery
    {
        public const string DateFormat = "yyyy-MM-dd";
    }

    public static class SearchStrategy
    {
        public const string LogName = "SearchStrategy";
        public static class Strategies
        {
            public const string Strategy1 = "strategy1";
            public const string Strategy2 = "strategy2";
            public const string Strategy3 = "strategy3";
        }
    }
}