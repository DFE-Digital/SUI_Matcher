using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public abstract class CsvFileProcessorBase(ILogger<CsvFileProcessorBase> logger, IStats stats)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    public async Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var ts = $"_{DateTime.Now:yyyyMMdd-HHmmss}";

        var outputDirectory =
            Path.Combine(outputPath, string.Concat(ts, "__", Path.GetFileNameWithoutExtension(filePath)));
        Directory.CreateDirectory(outputDirectory);

        (HashSet<string> headers, List<Dictionary<string, string>> records) = await ReadCsvAsync(filePath);

        AddExtraCsvHeaders(headers);

        int totalRecords = records.Count;
        int currentRecord = 0;
        var progressStopwatch = new Stopwatch();
        progressStopwatch.Start();

        logger.LogInformation("Beginning to process {TotalRecords} records from file: {FilePath}", totalRecords,
            filePath);

        foreach (var record in records)
        {
            currentRecord++;
            // Log progress at least every 5 seconds so we can see how many records are being processed over time.
            if (progressStopwatch.ElapsedMilliseconds >= 5000)
            {
                logger.LogInformation("{Current} of {Total} records processed", currentRecord, totalRecords);
                progressStopwatch.Restart();
            }

            await ProcessRecord(record, stats);
            // this delay is to try and stop requests getting throttled by the FHIR api.
            await Task.Delay(250);
        }

        progressStopwatch.Stop();

        await CreateMatchedCsvIfEnabled(filePath, ts, records, headers);

        var outputFilePath = GetOutputFileName(ts, outputDirectory, filePath);
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, headers, records);


        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, stats);
        var pdfReport = GeneratePdfReport(stats, ts, outputDirectory);
        var csvResult = new ProcessCsvFileResult(outputFilePath, statsJsonFileName, pdfReport, stats, outputDirectory);
        stats.ResetStats();
        return csvResult;
    }

    public static async Task<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> ReadCsvAsync(
        string filePath)
    {
        var headers = new HashSet<string>();
        var records = new List<Dictionary<string, string>>();

        if (!await IsFileReadyAsync(filePath))
        {
            throw new IOException($"File {filePath} is not ready for reading.");
        }

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader,
                   new CsvConfiguration(CultureInfo.InvariantCulture)
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

    /// <summary>
    /// Writes a CSV file asynchronously with the provided headers and records.
    /// The output file name is based on the input file name, suffixed with "_output_{timestamp}".
    /// </summary>
    public static async Task<string> WriteCsvAsync(string fileName, HashSet<string> headers,
        List<Dictionary<string, string>> records)
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

    protected abstract Task ProcessRecord(Dictionary<string, string> record, IStats stats);

    protected abstract void AddExtraCsvHeaders(HashSet<string> headers);

    protected abstract Task CreateMatchedCsvIfEnabled(string filePath, string ts,
        List<Dictionary<string, string>> records, HashSet<string> headers);

    protected abstract string GeneratePdfReport(IStats stats, string ts, string outputDirectory);

    protected static string GetOutputFileName(string timestamp, string outputDirectory, string fileName)
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_output_{timestamp}{extension}");
    }

    protected static string GetOutputFileName(string timestamp, string outputDirectory, string fileName,
        string fileSuffix)
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_{fileSuffix}_output_{timestamp}{extension}");
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

    private static string WriteStatsJsonFile(string outputDirectory, string ts, object stats)
    {
        var statsJsonFileName = GetOutputFileName(ts, outputDirectory, "stats.json");
        File.WriteAllText(statsJsonFileName, JsonSerializer.Serialize(stats, JsonSerializerOptions));
        return statsJsonFileName;
    }

    public static async Task<Dictionary<string, int>> ReadStatsJsonFileAsync(string statsFilePath)
    {
        if (!File.Exists(statsFilePath))
        {
            throw new FileNotFoundException("Stats file not found", statsFilePath);
        }

        var jsonString = await File.ReadAllTextAsync(statsFilePath);
        var statsData = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonString);

        return statsData ?? new Dictionary<string, int>();
    }
}