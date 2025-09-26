namespace SUI.Client.Core.Watcher;

public class CsvWatcherConfig
{
    public string IncomingDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming");
    public string ProcessedDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Processed");
    public string? MatchedRecordsDirectory { get; set; }
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int ProcessingDelayMs { get; set; } = 500;
    public bool EnableGenderSearch { get; set; } = false;
    public string? SearchStrategy { get; set; }
}