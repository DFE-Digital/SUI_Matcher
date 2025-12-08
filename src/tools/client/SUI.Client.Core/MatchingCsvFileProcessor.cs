using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Models;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using SUI.Client.Core.Watcher;

namespace SUI.Client.Core;

public class MatchingCsvFileProcessor(
    ILogger<MatchingCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchPersonApiService matchPersonApi,
    IOptions<CsvWatcherConfig> watcherConfig) : ICsvFileProcessor
{
    private readonly IStats _stats = new MatchingCsvProcessStats();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    private async Task ProcessRecord(Dictionary<string, string> record, IStats stats)
    {
        string gender = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)]).ToLower();

        if (int.TryParse(gender, out int _))
        {
            var genderFromNumber = PersonSpecificationUtils.ToGenderFromNumber(gender);
            gender = genderFromNumber;
            // Update the record with the string representation
            record[nameof(SearchQuery.Gender)] = genderFromNumber;
        }

        MatchPersonPayload payload = new()
        {
            Given = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Given)]),
            Family = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Family)]),
            BirthDate =
                record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.BirthDate)]),
            Email = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Email)]),
            AddressPostalCode =
                record.GetFirstValueOrDefault(
                    mapping.ColumnMappings[nameof(MatchPersonPayload.AddressPostalCode)]),
            Gender = watcherConfig.Value.EnableGenderSearch ? gender : null,
            OptionalProperties = GetOptionalFields(record),
            SearchStrategy = watcherConfig.Value.SearchStrategy,
            StrategyVersion = watcherConfig.Value.StrategyVersion

        };

        var response = await matchPersonApi.MatchPersonAsync(payload);

        record[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
        record[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
        record[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";

        RecordStats((MatchingCsvProcessStats)stats, response);
    }

    private void AddExtraCsvHeaders(HashSet<string> headers)
    {
        headers.Add(HeaderStatus);
        headers.Add(HeaderScore);
        headers.Add(HeaderNhsNo);
    }

    /// <summary>
    /// Creates a new file with only 'Match' status into a specified directory
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="ts"></param>
    /// <param name="records"></param>
    /// <param name="headers"></param>
    private async Task CreateMatchedCsvIfEnabled(string filePath, string ts, List<Dictionary<string, string>> records, HashSet<string> headers)
    {
        if (!string.IsNullOrEmpty(watcherConfig.Value.MatchedRecordsDirectory))
        {
            Directory.CreateDirectory(watcherConfig.Value.MatchedRecordsDirectory);

            var successOutputFilePath = GetOutputFileName(ts, watcherConfig.Value.MatchedRecordsDirectory, Path.GetFileName(filePath), "matched");

            var matchedRecords = records
                .Where(x => x.TryGetValue(HeaderStatus, out var status) && status == nameof(MatchStatus.Match))
                .ToList();

            // We only want to include matched records for under 19s in the output file.
            // As we cannot guarantee the date format in the input file, we will attempt to parse using a range of common UK date formats.
            // IF we encounter another format in the future, we can add configuration to specify which format to use.
            var birthDateColumn = mapping.ColumnMappings[nameof(MatchPersonPayload.BirthDate)]
                .FirstOrDefault(headers.Contains);

            var underNineteens = matchedRecords
                .Where(x => birthDateColumn != null
                            && x.TryGetValue(birthDateColumn, out var dobStr)
                            && DateOnly.TryParseExact(dobStr, ClientConstants.AcceptedCsvDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob)
                            && PersonSpecificationUtils.IsAgeEighteenOrUnder(dob))
                .ToList();
            logger.LogInformation("Writing matched records CSV file to: {SuccessOutputFilePath}. Matched record count {Count}", successOutputFilePath, underNineteens.Count);
            await ReconciliationCsvFileProcessor.WriteCsvAsync(successOutputFilePath, headers, underNineteens);
        }
    }

    private string GeneratePdfReport(IStats stats, string ts, string outputDirectory)
    {
        var localStats = (MatchingCsvProcessStats)stats;
        string[] categories = ["Errored", "Matched", "Potential Match", "Low confidence Match", "Many Match", "No Match"];
        double[] values = [localStats.ErroredCount, localStats.CountMatched, localStats.CountPotentialMatch, localStats.CountLowConfidenceMatch, localStats.CountManyMatch, localStats.CountNoMatch];
        return PdfReportGenerator.GenerateReport(GetOutputFileName(ts, outputDirectory, "report.pdf"),
            "CSV Processing Report", categories, values);
    }

    private static Dictionary<string, object> GetOptionalFields(Dictionary<string, string> record)
    {
        // As we cannot guarantee the presence of these fields in the CSV, we will check and only add them if they exist and are non-empty.
        var optionalFields = new Dictionary<string, object>();
        var activeCin = record.GetFirstValueOrDefault(["ActiveCIN"]);
        var activeCla = record.GetFirstValueOrDefault(["ActiveCLA"]);
        var activeCp = record.GetFirstValueOrDefault(["ActiveCP"]);
        var activeEhm = record.GetFirstValueOrDefault(["ActiveEHM"]);
        var ethnicity = record.GetFirstValueOrDefault(["Ethnicity"]);
        var immigrationStatus = record.GetFirstValueOrDefault(["ImmigrationStatus"]);
        if (!string.IsNullOrWhiteSpace(activeCin))
        {
            optionalFields.TryAdd("ActiveCIN", activeCin);
        }
        if (!string.IsNullOrWhiteSpace(activeCla))
        {
            optionalFields.TryAdd("ActiveCLA", activeCla);
        }
        if (!string.IsNullOrWhiteSpace(activeCp))
        {
            optionalFields.TryAdd("ActiveCP", activeCp);
        }
        if (!string.IsNullOrWhiteSpace(activeEhm))
        {
            optionalFields.TryAdd("ActiveEHM", activeEhm);
        }

        if (!string.IsNullOrWhiteSpace(ethnicity))
        {
            optionalFields.TryAdd("Ethnicity", ethnicity);
        }

        if (!string.IsNullOrWhiteSpace(immigrationStatus))
        {
            optionalFields.TryAdd("ImmigrationStatus", immigrationStatus);
        }
        return optionalFields;
    }

    private static void RecordStats(MatchingCsvProcessStats stats, PersonMatchResponse? response)
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
            case MatchStatus.LowConfidenceMatch:
                stats.CountLowConfidenceMatch++;
                break;
            case MatchStatus.Error:
                stats.ErroredCount++;
                break;
        }
    }

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

            await ProcessRecord(record, _stats);
            // this delay is to try and stop requests getting throttled by the FHIR api.
            await Task.Delay(250);
        }

        progressStopwatch.Stop();

        await CreateMatchedCsvIfEnabled(filePath, ts, records, headers);

        var outputFilePath = GetOutputFileName(ts, outputDirectory, filePath);
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, headers, records);


        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, _stats);
        var pdfReport = GeneratePdfReport(_stats, ts, outputDirectory);
        var csvResult = new ProcessCsvFileResult(outputFilePath, statsJsonFileName, pdfReport, _stats, outputDirectory);
        _stats.ResetStats();
        return csvResult;
    }

    private static async Task<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> ReadCsvAsync(
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
    private static async Task WriteCsvAsync(string fileName, HashSet<string> headers,
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
    }

    private static string GetOutputFileName(string timestamp, string outputDirectory, string fileName)
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_output_{timestamp}{extension}");
    }

    private static string GetOutputFileName(string timestamp, string outputDirectory, string fileName,
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