namespace SUI.Client.Core;

public class ProcessCsvFileResult(string outputCsvFile, string statsJsonFile, string reportPdfFile, CsvProcessStats stats)
{
    public string OutputCsvFile { get; set; } = outputCsvFile;
    public string StatsJsonFile { get; set; } = statsJsonFile;
    public string ReportPdfFile { get; set; } = reportPdfFile;
    public CsvProcessStats Stats { get; set; } = stats;
}