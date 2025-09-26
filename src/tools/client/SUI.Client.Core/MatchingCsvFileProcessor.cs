using System.Diagnostics;
using System.Globalization;

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
    IOptions<CsvWatcherConfig> watcherConfig) : CsvFileProcessorBase(logger, new MatchingCsvProcessStats()), ICsvFileProcessor
{
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    protected override async Task ProcessRecord(Dictionary<string, string> record, IStats stats)
    {
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
            SearchStrategy = watcherConfig.Value.SearchStrategy
        };

        var response = await matchPersonApi.MatchPersonAsync(payload);

        record[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
        record[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
        record[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";

        RecordStats((MatchingCsvProcessStats)stats, response);
    }

    protected override void AddExtraCsvHeaders(HashSet<string> headers)
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
    protected override async Task CreateMatchedCsvIfEnabled(string filePath, string ts, List<Dictionary<string, string>> records, HashSet<string> headers)
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
            await WriteCsvAsync(successOutputFilePath, headers, underNineteens);
        }
    }

    protected override string GeneratePdfReport(IStats stats, string ts, string outputDirectory)
    {
        var localStats = (MatchingCsvProcessStats)stats;
        string[] categories = ["Errored", "Matched", "Potential Match", "Many Match", "No Match"];
        double[] values = [localStats.ErroredCount, localStats.CountMatched, localStats.CountPotentialMatch, localStats.CountManyMatch, localStats.CountNoMatch];
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
            case MatchStatus.Error:
                stats.ErroredCount++;
                break;
        }
    }
}