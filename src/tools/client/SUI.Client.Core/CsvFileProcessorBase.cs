using System.Globalization;
using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

namespace SUI.Client.Core;

public abstract class CsvFileProcessorBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    protected static string WriteStatsJsonFile(string outputDirectory, string ts, object stats)
    {
        var statsJsonFileName = MatchingCsvFileProcessor.GetOutputFileName(ts, outputDirectory, "stats.json");
        File.WriteAllText(statsJsonFileName, JsonSerializer.Serialize(stats, MatchingCsvFileProcessor.JsonSerializerOptions));
        return statsJsonFileName;
    }

    protected static string GetOutputFileName(string timestamp, string outputDirectory, string fileName)
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_output_{timestamp}{extension}");
    }

    public static async Task<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> ReadCsvAsync(string filePath)
    {
        var headers = new HashSet<string>();
        var records = new List<Dictionary<string, string>>();

        if (!await MatchingCsvFileProcessor.IsFileReadyAsync(filePath))
        {
            throw new IOException($"File {filePath} is not ready for reading.");
        }

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            HeaderValidated = null
        }))
        {
            await csv.ReadAsync();
            csv.ReadHeader();

            if (csv.HeaderRecord is not null)
            {
                headers.UnionWith(csv.HeaderRecord);
            }

            while (await csv.ReadAsync())
            {
                var row = new Dictionary<string, string>();
                foreach (var header in headers)
                {
                    row[header] = csv.GetField(header) ?? string.Empty;
                }
                records.Add(row);
            }
        }

        return (headers, records);
    }

    private static async Task<bool> IsFileReadyAsync(string filePath, int maxAttempts = 5, int delayMs = 1000)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                await Task.Delay(delayMs);
            }
        }
        return false;
    }

    /// <summary>
    /// Writes a CSV file asynchronously with the provided headers and records.
    /// The output file name is based on the input file name, suffixed with "_output_{timestamp}".
    /// </summary>
    public static async Task<string> WriteCsvAsync(string fileName, HashSet<string> headers, List<Dictionary<string, string>> records)
    {
        await using var writer = new StreamWriter(fileName);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        await csv.NextRecordAsync();


        foreach (var record in records)
        {
            foreach (var header in headers)
            {
                csv.WriteField(record.GetValueOrDefault(header, ""));
            }
            await csv.NextRecordAsync();
        }

        return fileName;
    }
}