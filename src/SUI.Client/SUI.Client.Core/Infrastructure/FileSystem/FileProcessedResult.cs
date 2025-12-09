namespace SUI.Client.Core.Infrastructure.FileSystem;

public class FileProcessedEnvelope(string inputFile, ProcessCsvFileResult? result = null, Exception? exception = null)
{
    public string InputFile { get; set; } = inputFile;
    private ProcessCsvFileResult? Result { get; } = result;
    public Exception? Exception { get; set; } = exception;
    public bool Success => Exception == null;
    public ProcessCsvFileResult AssertSuccess() => Result ?? throw new ArgumentNullException(nameof(result));
}