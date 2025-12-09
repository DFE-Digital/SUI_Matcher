namespace SUI.Client.Core.Infrastructure.FileSystem;

public interface ICsvFileProcessor
{
    Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath);
}