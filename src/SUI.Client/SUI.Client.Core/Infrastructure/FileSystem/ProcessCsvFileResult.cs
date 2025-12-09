namespace SUI.Client.Core.Infrastructure.FileSystem;

public class ProcessCsvFileResult(string outputCsvFile, string statsJsonFile, string reportPdfFile, IStats stats, string outputDirectory)
{
    public string OutputCsvFile { get; set; } = outputCsvFile;
    public string StatsJsonFile { get; set; } = statsJsonFile;
    public string ReportPdfFile { get; set; } = reportPdfFile;
    public string OutputDirectory { get; set; } = outputDirectory;
    public IStats Stats { get; set; } = stats;
}