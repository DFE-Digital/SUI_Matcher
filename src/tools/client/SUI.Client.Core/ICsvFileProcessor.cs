namespace SUI.Client.Core;

public interface ICsvFileProcessor
{
    Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath);
}