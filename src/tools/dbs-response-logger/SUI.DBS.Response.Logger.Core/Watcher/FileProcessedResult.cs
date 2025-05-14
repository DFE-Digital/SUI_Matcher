namespace SUI.DBS.Response.Logger.Core.Watcher;

public class FileProcessedEnvelope(string inputFile, Exception? exception = null)
{
    public string InputFile { get; set; } = inputFile;
    public Exception? Exception { get; set; } = exception;
    public bool Success => Exception == null;
}