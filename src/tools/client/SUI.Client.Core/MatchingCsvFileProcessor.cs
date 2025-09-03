using System.Diagnostics;

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
    IOptions<CsvWatcherConfig> watcherConfig) : CsvFileProcessorBase, ICsvFileProcessor
{
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    public async Task<ProcessCsvFileResult> ProcessCsvFileAsync(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var ts = $"_{DateTime.Now:yyyyMMdd-HHmmss}";

        var outputDirectory = Path.Combine(outputPath, string.Concat(ts, "__", Path.GetFileNameWithoutExtension(filePath)));
        Directory.CreateDirectory(outputDirectory);

        var stats = new MatchingCsvProcessStats();
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

            string? gender = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)]).ToLower();

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
            };

            var response = await matchPersonApi.MatchPersonAsync(payload);

            record[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
            record[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
            record[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";

            RecordStats(stats, response);
        }

        progressStopwatch.Stop();

        await CreateMatchedCsvIfEnabled(filePath, ts, records, headers);

        var outputFilePath = GetOutputFileName(ts, outputDirectory, filePath);
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, headers, records);

        string[] categories = { "Errored", "Matched", "Potential Match", "Many Match", "No Match" };
        double[] values = { stats.ErroredCount, stats.CountMatched, stats.CountPotentialMatch, stats.CountManyMatch, stats.CountNoMatch };
        var pdfReport = PdfReportGenerator.GenerateReport(GetOutputFileName(ts, outputDirectory, "report.pdf"), "CSV Processing Report", categories, values);
        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, stats);

        return new ProcessCsvFileResult(outputFilePath, statsJsonFileName, pdfReport, stats, outputDirectory);
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
            var matchedRecords = records.Where(x => x.TryGetValue(HeaderStatus, out var status) && status == nameof(MatchStatus.Match)).ToList();
            logger.LogInformation("Writing matched records CSV file to: {SuccessOutputFilePath}. Matched record count {Count}", successOutputFilePath, matchedRecords.Count);
            await WriteCsvAsync(successOutputFilePath, headers, matchedRecords);
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
            case MatchStatus.Error:
                stats.ErroredCount++;
                break;
        }
    }
}