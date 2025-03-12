namespace SUI.Client.Core.Watcher;

public class FileProcessedEnvelope(string inputFile, ProcessCsvFileResult? result = null, Exception? exception = null)
{
    public string InputFile { get; set; } = inputFile;
    public ProcessCsvFileResult? Result { get; } = result;
    public Exception? Exception { get; set; } = exception;
    public bool Success => Exception == null;
    public ProcessCsvFileResult AssertSuccess() => Result ?? throw new Exception("The result is null");
}