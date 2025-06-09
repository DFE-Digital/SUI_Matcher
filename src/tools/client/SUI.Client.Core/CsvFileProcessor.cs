using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

using Shared.Models;

using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;

using Types = Shared.Models;

namespace SUI.Client.Core;

public interface ICsvFileProcessor
{
    Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath);
}

public class CsvFileProcessor(ILogger<CsvFileProcessor> logger, CsvMappingConfig mapping, IMatchPersonApiService matchPersonApi) : ICsvFileProcessor
{
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    public async Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var ts = $"_{DateTime.Now:yyyyMMdd-HHmmss}";

        var outputDirectory = Path.Combine(outputPath, string.Concat(ts, "__", Path.GetFileNameWithoutExtension(filePath)));
        Directory.CreateDirectory(outputDirectory);

        var stats = new CsvProcessStats();
        (HashSet<string> headers, List<Dictionary<string, string>> records) = await ReadCsvAsync(filePath);

        headers.Add(HeaderStatus);
        headers.Add(HeaderScore);
        headers.Add(HeaderNhsNo);

        int totalRecords = records.Count;
        int currentRecord = 0;
        var progressStopwatch = new Stopwatch();
        progressStopwatch.Start();

        logger.LogInformation("Beginning to process {TotalRecords} records from file: {FilePath}", totalRecords, filePath);

        foreach (var record in records)
        {
            currentRecord++;
            // Log progress at least every 5 seconds so we can see how many records are being processed over time.
            if (progressStopwatch.ElapsedMilliseconds >= 5000)
            {
                logger.LogInformation("{Current} of {Total} records processed", currentRecord, totalRecords);
                progressStopwatch.Restart();
            }

            var payload = new MatchPersonPayload
            {
                Given = record.GetValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Given)]),
                Family = record.GetValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Family)]),
                BirthDate = record.GetValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.BirthDate)]),
                Email = record.GetValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Email)]),
                AddressPostalCode = record.GetValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.AddressPostalCode)]),
                Gender = record.GetValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)]),
            };

            var response = await matchPersonApi.MatchPersonAsync(payload);

            record[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
            record[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
            record[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";

            RecordStats(stats, response);
        }

        var outputFilePath = GetOutputFileName(ts, outputDirectory, filePath);
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, headers, records);

        var pdfReport = PdfReportGenerator.GenerateReport(stats, GetOutputFileName(ts, outputDirectory, "report.pdf"));
        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, stats);

        return new ProcessCsvFileResult(outputFilePath, statsJsonFileName, pdfReport, stats, outputDirectory);
    }

    private static string WriteStatsJsonFile(string outputDirectory, string ts, CsvProcessStats stats)
    {
        var statsJsonFileName = GetOutputFileName(ts, outputDirectory, "stats.json");
        File.WriteAllText(statsJsonFileName, JsonSerializer.Serialize(stats, JsonSerializerOptions));
        return statsJsonFileName;
    }

    private static void RecordStats(CsvProcessStats stats, PersonMatchResponse? response)
    {
        stats.Count++;
        switch (response?.Result?.MatchStatus)
        {
            case MatchStatus.Match:
                stats.CountMatched++;
                break;

            case MatchStatus.ManyMatch:
                stats.CountManyMatch++;
                break;

            case MatchStatus.NoMatch:
                stats.CountNoMatch++;
                break;

            case MatchStatus.PotentialMatch:
                stats.CountPotentialMatch++;
                break;
            case MatchStatus.Error:
                stats.ErroredCount++;
                break;
        }
    }

    private static string GetOutputFileName(string timestamp, string outputDirectory, string fileName)
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_output_{timestamp}{extension}");
    }

    public static async Task<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> ReadCsvAsync(string filePath)
    {
        var headers = new HashSet<string>();
        var records = new List<Dictionary<string, string>>();

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