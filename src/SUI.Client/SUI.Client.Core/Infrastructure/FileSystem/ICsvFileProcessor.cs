using System.Data;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public interface ICsvFileProcessor
{
    Task<ProcessCsvFileResult> ProcessCsvFileAsync(DataTable inputData, string outputPath);
}