using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Util;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public class MatchingCsvFileProcessor(
    ILogger<MatchingCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchingService matching,
    IOptions<CsvWatcherConfig> watcherConfig
) : ICsvFileProcessor
{
    private readonly MatchingProcessStats _stats = new();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
    };
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    private async Task ProcessRecord(Dictionary<string, string> record, MatchingProcessStats stats)
    {
        string gender = record
            .GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)])
            .ToLower();

        if (int.TryParse(gender, out int _))
        {
            var genderFromNumber = PersonSpecificationUtils.ToGenderFromNumber(gender);
            gender = genderFromNumber;
            // Update the record with the string representation
            record[nameof(SearchQuery.Gender)] = genderFromNumber;
        }

        MatchPersonPayload payload = new()
        {
            Given = record.GetFirstValueOrDefault(
                mapping.ColumnMappings[nameof(MatchPersonPayload.Given)]
            ),
            Family = record.GetFirstValueOrDefault(
                mapping.ColumnMappings[nameof(MatchPersonPayload.Family)]
            ),
            BirthDate = record.GetFirstValueOrDefault(
                mapping.ColumnMappings[nameof(MatchPersonPayload.BirthDate)]
            ),
            Email = record.GetFirstValueOrDefault(
                mapping.ColumnMappings[nameof(MatchPersonPayload.Email)]
            ),
            AddressPostalCode = record.GetFirstValueOrDefault(
                mapping.ColumnMappings[nameof(MatchPersonPayload.AddressPostalCode)]
            ),
            Gender = watcherConfig.Value.EnableGenderSearch ? gender : null,
            OptionalProperties = GetOptionalFields(record),
            SearchStrategy = watcherConfig.Value.SearchStrategy,
            StrategyVersion = watcherConfig.Value.StrategyVersion,
        };

        var response = await matching.MatchPersonAsync(payload);

        record[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
        record[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
        record[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";

        stats.RecordStats(response);
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
    private async Task CreateMatchedCsvIfEnabled(
        string filePath,
        string ts,
        List<Dictionary<string, string>> records,
        HashSet<string> headers
    )
    {
        if (!string.IsNullOrEmpty(watcherConfig.Value.MatchedRecordsDirectory))
        {
            Directory.CreateDirectory(watcherConfig.Value.MatchedRecordsDirectory);

            var successOutputFilePath = GetOutputFileName(
                ts,
                watcherConfig.Value.MatchedRecordsDirectory,
                Path.GetFileName(filePath),
                "matched"
            );

            var matchedRecords = records
                .Where(x =>
                    x.TryGetValue(HeaderStatus, out var status)
                    && status == nameof(MatchStatus.Match)
                )
                .ToList();

            // We only want to include matched records for under 19s in the output file.
            // As we cannot guarantee the date format in the input file, we will attempt to parse using a range of common UK date formats.
            // IF we encounter another format in the future, we can add configuration to specify which format to use.
            var birthDateColumn = mapping
                .ColumnMappings[nameof(MatchPersonPayload.BirthDate)]
                .FirstOrDefault(headers.Contains);

            var underNineteens = matchedRecords
                .Where(x =>
                    birthDateColumn != null
                    && x.TryGetValue(birthDateColumn, out var dobStr)
                    && DateOnly.TryParseExact(
                        dobStr,
                        AcceptedCsvDateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dob
                    )
                    && PersonSpecificationUtils.IsAgeEighteenOrUnder(dob)
                )
                .ToList();
            logger.LogInformation(
                "Writing matched records CSV file to: {SuccessOutputFilePath}. Matched record count {Count}",
                successOutputFilePath,
                underNineteens.Count
            );
            await WriteCsvAsync(successOutputFilePath, headers, underNineteens);
        }
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

    public async Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var ts = $"_{DateTime.Now:yyyyMMdd-HHmmss}";

        var outputDirectory = Path.Combine(
            outputPath,
            string.Concat(ts, "__", Path.GetFileNameWithoutExtension(filePath))
        );
        Directory.CreateDirectory(outputDirectory);

        var csvData = await CsvRecordReader.ReadCsvFileAsync(filePath);

        AddExtraCsvHeaders(csvData.Headers);

        int totalRecords = csvData.Records.Count;
        int currentRecord = 0;
        var progressStopwatch = new Stopwatch();
        progressStopwatch.Start();

        logger.LogInformation(
            "Beginning to process {TotalRecords} records from file: {FilePath}",
            totalRecords,
            filePath
        );

        foreach (var record in csvData.Records)
        {
            currentRecord++;
            // Log progress at least every 5 seconds so we can see how many records are being processed over time.
            if (progressStopwatch.ElapsedMilliseconds >= 5000)
            {
                logger.LogInformation(
                    "{Current} of {Total} records processed",
                    currentRecord,
                    totalRecords
                );
                progressStopwatch.Restart();
            }

            await ProcessRecord(record, _stats);
            // this delay is to try and stop requests getting throttled by the FHIR api.
            await Task.Delay(250);
        }

        progressStopwatch.Stop();

        await CreateMatchedCsvIfEnabled(filePath, ts, csvData.Records, csvData.Headers);

        var outputFilePath = GetOutputFileName(ts, outputDirectory, filePath);
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, csvData.Headers, csvData.Records);

        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, _stats);
        var csvResult = new ProcessCsvFileResult(
            outputFilePath,
            statsJsonFileName,
            _stats,
            outputDirectory
        );
        _stats.ResetStats();
        return csvResult;
    }

    /// <summary>
    /// Writes a CSV file asynchronously with the provided headers and records.
    /// The output file name is based on the input file name, suffixed with "_output_{timestamp}".
    /// </summary>
    private static async Task WriteCsvAsync(
        string fileName,
        HashSet<string> headers,
        List<Dictionary<string, string>> records
    )
    {
        await using var writer = new StreamWriter(fileName);
        await using var csv = new CsvWriter(
            writer,
            new CsvConfiguration(CultureInfo.InvariantCulture)
        );

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

    private static string GetOutputFileName(
        string timestamp,
        string outputDirectory,
        string fileName
    )
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_output_{timestamp}{extension}");
    }

    private static string GetOutputFileName(
        string timestamp,
        string outputDirectory,
        string fileName,
        string fileSuffix
    )
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(
            outputDirectory,
            $"{filenameWithoutExt}_{fileSuffix}_output_{timestamp}{extension}"
        );
    }

    private static string WriteStatsJsonFile(string outputDirectory, string ts, object stats)
    {
        var statsJsonFileName = GetOutputFileName(ts, outputDirectory, "stats.json");
        File.WriteAllText(
            statsJsonFileName,
            JsonSerializer.Serialize(stats, JsonSerializerOptions)
        );
        return statsJsonFileName;
    }

    public static readonly string[] AcceptedCsvDateFormats =
    [
        "yyyy-MM-dd",
        "yyyyMMdd",
        "yyyy/MM/dd",
    ];
}
