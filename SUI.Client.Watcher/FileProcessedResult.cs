namespace SUI.Client.Watcher;

public class FileProcessedResult(string inputFile, string? outputFile = null, Exception? exception = null)
{
    public string InputFile { get; set; } = inputFile;
    public string? OutputFile { get; } = outputFile;
    public Exception? Exception { get; set; } = exception;
    public bool Success => Exception == null;
}