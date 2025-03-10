namespace SUI.Client.Core;

public class ProcessCsvFileResult(string outputCsvFile, string statsJsonFile, string reportPdfFile, CsvProcessStats stats, string outputDirectory)
{
    public string OutputCsvFile { get; set; } = outputCsvFile;
    public string StatsJsonFile { get; set; } = statsJsonFile;
    public string ReportPdfFile { get; set; } = reportPdfFile;
    public string OutputDirectory { get; set; } = outputDirectory;
    public CsvProcessStats Stats { get; set; } = stats;
}