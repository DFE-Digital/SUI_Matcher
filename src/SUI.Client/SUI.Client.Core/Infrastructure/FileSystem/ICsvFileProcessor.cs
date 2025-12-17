namespace SUI.Client.Core.Infrastructure.FileSystem;

public interface ICsvFileProcessor
{
    Task<ProcessCsvFileResult> ProcessCsvFileAsync(string tableName, HashSet<string> headers, List<Dictionary<string, string>> records, string outputPath);
}