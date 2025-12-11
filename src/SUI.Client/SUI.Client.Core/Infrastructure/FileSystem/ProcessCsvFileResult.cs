namespace SUI.Client.Core.Infrastructure.FileSystem;

public record ProcessCsvFileResult(string OutputCsvFile, string StatsJsonFile, IStats Stats, string OutputDirectory);