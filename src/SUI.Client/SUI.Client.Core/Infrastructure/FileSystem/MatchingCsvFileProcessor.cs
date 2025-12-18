using System.Data;
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

namespace SUI.Client.Core.Infrastructure.FileSystem;

public class MatchingCsvFileProcessor(
    ILogger<MatchingCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchingService matching,
    IOptions<CsvWatcherConfig> watcherConfig) : ICsvFileProcessor
{
    private readonly IStats _stats = new MatchingCsvProcessStats();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    private async Task ProcessRecord(DataRow row, IStats stats)
    {
        string gender = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)]).ToLower();

        if (int.TryParse(gender, out int _))
        {
            var genderFromNumber = PersonSpecificationUtils.ToGenderFromNumber(gender);
            gender = genderFromNumber;
            // Update the record with the string representation
            row[nameof(SearchQuery.Gender)] = genderFromNumber;
        }

        MatchPersonPayload payload = new()
        {
            Given = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Given)]),
            Family = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Family)]),
            BirthDate =
                row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.BirthDate)]),
            Email = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Email)]),
            AddressPostalCode =
                row.GetFirstValueOrDefault(
                    mapping.ColumnMappings[nameof(MatchPersonPayload.AddressPostalCode)]),
            Gender = watcherConfig.Value.EnableGenderSearch ? gender : null,
            OptionalProperties = GetOptionalFields(row),
            SearchStrategy = watcherConfig.Value.SearchStrategy,
            StrategyVersion = watcherConfig.Value.StrategyVersion

        };

        var response = await matching.MatchPersonAsync(payload);

        row[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
        row[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
        row[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";

        RecordStats((MatchingCsvProcessStats)stats, response);
    }

    private void AddExtraCsvHeaders(DataTable inputData)
    {
        inputData.Columns.Add(HeaderStatus);
        inputData.Columns.Add(HeaderScore);
        inputData.Columns.Add(HeaderNhsNo);
    }

    /// <summary>
    /// Creates a new file with only 'Match' status into a specified directory
    /// </summary>
    /// <param name="data"></param>
    /// <param name="ts"></param>
    private async Task CreateMatchedCsvIfEnabled(DataTable data, string ts)
    {
        if (!string.IsNullOrEmpty(watcherConfig.Value.MatchedRecordsDirectory))
        {
            Directory.CreateDirectory(watcherConfig.Value.MatchedRecordsDirectory);

            var successOutputFilePath = Path.Combine(watcherConfig.Value.MatchedRecordsDirectory, $"{data.TableName}_matched_output_{ts}.csv");

            var birthDateColumn = mapping.ColumnMappings[nameof(MatchPersonPayload.BirthDate)].Single(data.Columns.Contains);

            var outputData = data.AsEnumerable()
                // Confident matches
                .Where(row => row.Field<string>(HeaderStatus) == nameof(MatchStatus.Match))
                // Under 19s only
                .Where(row =>
                {
                    var birthDateStringValue = row.Field<string>(birthDateColumn);
                    if (string.IsNullOrWhiteSpace(birthDateStringValue)) return false;
                    var birthDateParsed = DateOnly.ParseExact(birthDateStringValue, AcceptedCsvDateFormats,
                        CultureInfo.InvariantCulture);
                    return PersonSpecificationUtils.IsAgeEighteenOrUnder(birthDateParsed);
                })
                .CopyToDataTable();

            logger.LogInformation("Writing matched records CSV file to: {SuccessOutputFilePath}. Matched record count {Count}", successOutputFilePath, outputData.Rows.Count);
            await WriteCsvAsync(successOutputFilePath, outputData);
        }
    }

    private static Dictionary<string, object> GetOptionalFields(DataRow row)
    {
        // As we cannot guarantee the presence of these fields in the CSV, we will check and only add them if they exist and are non-empty.
        var optionalFields = new Dictionary<string, object>();
        var activeCin = row.GetFirstValueOrDefault(["ActiveCIN"]);
        var activeCla = row.GetFirstValueOrDefault(["ActiveCLA"]);
        var activeCp = row.GetFirstValueOrDefault(["ActiveCP"]);
        var activeEhm = row.GetFirstValueOrDefault(["ActiveEHM"]);
        var ethnicity = row.GetFirstValueOrDefault(["Ethnicity"]);
        var immigrationStatus = row.GetFirstValueOrDefault(["ImmigrationStatus"]);
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

    public async Task<ProcessCsvFileResult> ProcessCsvFileAsync(DataTable inputData, string outputPath)
    {
        AddExtraCsvHeaders(inputData);

        int totalRecords = inputData.Rows.Count;
        int currentRecord = 0;
        var progressStopwatch = new Stopwatch();
        progressStopwatch.Start();

        logger.LogInformation("Beginning to process {TotalRecords} records from file: {TableName}", totalRecords,
            inputData.TableName);

        foreach (DataRow row in inputData.Rows)
        {
            currentRecord++;
            // Log progress at least every 5 seconds so we can see how many records are being processed over time.
            if (progressStopwatch.ElapsedMilliseconds >= 5000)
            {
                logger.LogInformation("{Current} of {Total} records processed", currentRecord, totalRecords);
                progressStopwatch.Restart();
            }

            await ProcessRecord(row, _stats);
            // this delay is to try and stop requests getting throttled by the FHIR api.
            await Task.Delay(250);
        }

        progressStopwatch.Stop();

        var ts = $"_{Process.GetCurrentProcess().StartTime:yyyyMMdd-HHmmss}";

        await CreateMatchedCsvIfEnabled(inputData, ts);

        var outputDirectory =
            Path.Combine(outputPath, string.Concat(ts, "__", inputData.TableName));
        Directory.CreateDirectory(outputDirectory);

        var outputFilePath = GetOutputFileName(ts, outputDirectory, inputData.TableName + ".csv");
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, inputData);

        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, _stats);
        var csvResult = new ProcessCsvFileResult(outputFilePath, statsJsonFileName, _stats, outputDirectory);
        _stats.ResetStats();
        return csvResult;
    }

    /// <summary>
    /// Writes a CSV file asynchronously with the provided headers and records.
    /// The output file name is based on the input file name, suffixed with "_output_{timestamp}".
    /// </summary>
    public static async Task WriteCsvAsync(string fileName, DataTable inputData)
    {
        await using var writer = new StreamWriter(fileName);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (DataColumn column in inputData.Columns)
        {
            csv.WriteField(column.ColumnName);
        }

        await csv.NextRecordAsync();

        foreach (DataRow row in inputData.Rows)
        {
            foreach (DataColumn column in inputData.Columns)
            {
                csv.WriteField(row.Field<string>(column));
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

    private static string WriteStatsJsonFile(string outputDirectory, string ts, object stats)
    {
        var statsJsonFileName = GetOutputFileName(ts, outputDirectory, "stats.json");
        File.WriteAllText(statsJsonFileName, JsonSerializer.Serialize(stats, JsonSerializerOptions));
        return statsJsonFileName;
    }

    public static readonly string[] AcceptedCsvDateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];
}