﻿namespace SUI.DBS.Response.Logger.Core.Watcher;

public class TxtWatcherConfig
{
    public string IncomingDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming");
    public string ProcessedDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Processed");
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int ProcessingDelayMs { get; set; } = 500;
}