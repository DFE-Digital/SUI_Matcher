using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared;
using Shared.Models;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using SUI.Client.Core.Watcher;

namespace SUI.Client.Core;

public class ReconciliationCsvFileProcessor(
    ILogger<ReconciliationCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchPersonApiService matchPersonApi,
    IOptions<CsvWatcherConfig> watcherConfig) : CsvFileProcessorBase(logger, new ReconciliationCsvProcessStats()), ICsvFileProcessor
{
    public const string HeaderNhsNo = "SUI_NHSNo";
    public const string HeaderGivenName = "SUI_GivenName";
    public const string HeaderAddressPostalCode = "SUI_PostalCode";
    public const string HeaderFamilyName = "SUI_FamilyName";
    public const string HeaderBirthDate = "SUI_BirthDate";
    public const string HeaderGender = "SUI_Gender";
    public const string HeaderEmail = "SUI_Email";
    public const string HeaderPhone = "SUI_Phone";
    public const string HeaderDifferences = "SUI_Differences";
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderMatchStatus = "SUI_MatchStatus";
    public const string HeaderMatchNhsNumber = "SUI_MatchNhsNumber";

    protected override async Task ProcessRecord(Dictionary<string, string> record, IStats stats)
    {
        string? gender = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)])
            .ToLower();

        if (int.TryParse(gender, out int _))
        {
            var genderFromNumber = PersonSpecificationUtils.ToGenderFromNumber(gender);
            gender = genderFromNumber;
            // Update the record with the string representation
            record[nameof(ReconciliationRequest.Gender)] = genderFromNumber;
        }

        var dob = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.BirthDate)]);
        ReconciliationRequest payload = new()
        {
            NhsNumber = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.NhsNumber)]),
            Given = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Given)]),
            Family = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Family)]),
            BirthDate =
                dob.ToDateOnly([Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish]),
            Email = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Email)]),
            AddressPostalCode =
                record.GetFirstValueOrDefault(
                    mapping.ColumnMappings[nameof(ReconciliationRequest.AddressPostalCode)]),
            Gender = gender,
            Phone = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Phone)]),
            SearchStrategy = watcherConfig.Value.SearchStrategy ?? SharedConstants.SearchStrategy.Strategies.Strategy1,
        };

        var response = await matchPersonApi.ReconcilePersonAsync(payload);

        record[HeaderNhsNo] = response?.Person?.NhsNumber ?? "-";
        record[HeaderGivenName] = string.Join(" - ", response?.Person?.GivenNames ?? ["-"]);
        record[HeaderFamilyName] = string.Join(" - ", response?.Person?.FamilyNames ?? ["-"]);
        record[HeaderBirthDate] = response?.Person?.BirthDate.ToString() ?? "-";
        record[HeaderGender] = response?.Person?.Gender ?? "-";
        record[HeaderAddressPostalCode] = string.Join(" - ", response?.Person?.AddressPostalCodes ?? ["-"]);
        record[HeaderEmail] = string.Join(" - ", response?.Person?.Emails ?? ["-"]);
        record[HeaderPhone] = string.Join(" - ", response?.Person?.PhoneNumbers ?? ["-"]);
        var differenceList = response?.DifferenceString ?? "-";
        record[HeaderDifferences] = differenceList;
        record[HeaderStatus] = response?.Status.ToString() ?? "-";
        record[HeaderMatchNhsNumber] = response?.MatchingResult?.NhsNumber ?? "-";
        record[HeaderMatchStatus] = response?.MatchingResult?.MatchStatus.ToString() ?? "-";

        RecordStats((ReconciliationCsvProcessStats)stats, response, differenceList);
    }

    protected override void AddExtraCsvHeaders(HashSet<string> headers)
    {
        headers.Add(HeaderNhsNo);
        headers.Add(HeaderGivenName);
        headers.Add(HeaderFamilyName);
        headers.Add(HeaderBirthDate);
        headers.Add(HeaderGender);
        headers.Add(HeaderAddressPostalCode);
        headers.Add(HeaderEmail);
        headers.Add(HeaderPhone);
        headers.Add(HeaderDifferences);
        headers.Add(HeaderStatus);
        headers.Add(HeaderMatchStatus);
        headers.Add(HeaderMatchNhsNumber);
    }

    protected override Task CreateMatchedCsvIfEnabled(string filePath, string ts, List<Dictionary<string, string>> records, HashSet<string> headers)
    {
        return Task.CompletedTask;
    }

    protected override string GeneratePdfReport(IStats stats, string ts, string outputDirectory)
    {
        var localStats = (ReconciliationCsvProcessStats)stats;
        string[] categories =
        [
            "Errored", "No Differences", "Superseded NHS Number", "Invalid NHS Number",
            "Patient Not Found", "Missing NHS Number", "Differences",
        ];
        double[] values =
        [
            localStats.ErroredCount, localStats.NoDifferenceCount, localStats.SupersededNhsNumber,
            localStats.InvalidNhsNumber, localStats.PatientNotFound, localStats.MissingNhsNumber, localStats.DifferencesCount,
        ];
        string[] differenceCategories =
        [
            "Birthdate Differences", "Birthdate Missing NHS", "Birthdate Missing LA", "Birthdate Missing Both",
            "Email Differences", "Email Missing NHS", "Email Missing LA", "Email Missing Both",
            "Phone Differences", "Phone Missing NHS", "Phone Missing LA", "Phone Missing Both",
            "Given Name Differences", "Given Name Missing NHS", "Given Name Missing LA", "Given Name Missing Both",
            "Family Name Differences", "Family Name Missing NHS", "Family Name Missing LA", "Family Name Missing Both",
            "Postcode Differences", "Postcode Missing NHS", "Postcode Missing LA", "Postcode Missing Both",
        ];
        double[] differenceValues =
        [
            localStats.BirthDateCount, localStats.BirthDateNhsCount, localStats.BirthDateLaCount, localStats.BirthDateBothCount,
            localStats.EmailCount, localStats.EmailNhsCount, localStats.EmailLaCount, localStats.EmailBothCount,
            localStats.PhoneCount, localStats.PhoneNhsCount, localStats.PhoneLaCount, localStats.PhoneBothCount,
            localStats.GivenNameCount, localStats.GivenNameNhsCount, localStats.GivenNameLaCount, localStats.GivenNameBothCount,
            localStats.FamilyNameCount, localStats.FamilyNameNhsCount, localStats.FamilyNameLaCount, localStats.FamilyNameBothCount,
            localStats.PostCodeCount, localStats.PostCodeNhsCount, localStats.PostCodeLaCount, localStats.PostCodeBothCount,
        ];
        string[] matchingCategories =
        [
            "Matching Status Match", "Matching Status Potential Match", "Matching Status No Match",
            "Matching Status Many Match", "Matching Status Error"
        ];
        double[] matchingValues =
        [
            localStats.MatchingStatusMatch, localStats.MatchingStatusPotentialMatch, localStats.MatchingStatusNoMatch,
            localStats.MatchingStatusManyMatch, localStats.MatchingStatusError
        ];
        return PdfReportGenerator.GenerateReconciliationReport(
            GetOutputFileName(ts, outputDirectory, "ReconciliationReport.pdf"),
            "Reconciliation Report",
            localStats.Count,
            new Tuple<string[], double[]>(categories, values),
            new Tuple<string[], double[]>(differenceCategories, differenceValues),
            new Tuple<string[], double[]>(matchingCategories, matchingValues));
    }

    private static void RecordStats(ReconciliationCsvProcessStats stats, ReconciliationResponse? response, string differenceList)
    {
        stats.Count++;
        switch (response?.MatchingResult?.MatchStatus)
        {
            case MatchStatus.Match:
                stats.MatchingStatusMatch++;
                break;
            case MatchStatus.NoMatch:
                stats.MatchingStatusNoMatch++;
                break;
            case MatchStatus.PotentialMatch:
                stats.MatchingStatusPotentialMatch++;
                break;
            case MatchStatus.ManyMatch:
                stats.MatchingStatusManyMatch++;
                break;
            default:
                stats.MatchingStatusError++;
                break;
        }

        switch (response?.Status)
        {
            case ReconciliationStatus.NoDifferences:
                stats.NoDifferenceCount++;
                break;
            case ReconciliationStatus.Differences:
                UpdateStatsForField(differenceList, stats, "BirthDate", s => s.BirthDateCount++, s => s.BirthDateNhsCount++, s => s.BirthDateLaCount++, s => s.BirthDateBothCount++);
                UpdateStatsForField(differenceList, stats, "Email", s => s.EmailCount++, s => s.EmailNhsCount++, s => s.EmailLaCount++, s => s.EmailBothCount++);
                UpdateStatsForField(differenceList, stats, "Phone", s => s.PhoneCount++, s => s.PhoneNhsCount++, s => s.PhoneLaCount++, s => s.PhoneBothCount++);
                UpdateStatsForField(differenceList, stats, "Given", s => s.GivenNameCount++, s => s.GivenNameNhsCount++, s => s.GivenNameLaCount++, s => s.GivenNameBothCount++);
                UpdateStatsForField(differenceList, stats, "Family", s => s.FamilyNameCount++, s => s.FamilyNameNhsCount++, s => s.FamilyNameLaCount++, s => s.FamilyNameBothCount++);
                UpdateStatsForField(differenceList, stats, "AddressPostalCode", s => s.PostCodeCount++, s => s.PostCodeNhsCount++, s => s.PostCodeLaCount++, s => s.PostCodeBothCount++);

                stats.DifferencesCount++;
                break;
            case ReconciliationStatus.SupersededNhsNumber:
                stats.SupersededNhsNumber++;
                break;
            case ReconciliationStatus.InvalidNhsNumber:
                stats.InvalidNhsNumber++;
                break;
            case ReconciliationStatus.PatientNotFound:
                stats.PatientNotFound++;
                break;
            case ReconciliationStatus.MissingNhsNumber:
                stats.MissingNhsNumber++;
                break;
            default:
                stats.ErroredCount++;
                break;
        }
    }

    private static void UpdateStatsForField(
        string differenceList,
        ReconciliationCsvProcessStats stats,
        string fieldName,
        Action<ReconciliationCsvProcessStats> incrementPlain,
        Action<ReconciliationCsvProcessStats> incrementNhs,
        Action<ReconciliationCsvProcessStats> incrementLa,
        Action<ReconciliationCsvProcessStats> incrementBoth)
    {
        var plainRegex = new Regex($@"\b{fieldName}\b(?!:)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10));

        if (plainRegex.IsMatch(differenceList)) { incrementPlain(stats); }
        if (differenceList.Contains($"{fieldName}:NHS")) { incrementNhs(stats); }
        if (differenceList.Contains($"{fieldName}:LA")) { incrementLa(stats); }
        if (differenceList.Contains($"{fieldName}:Both")) { incrementBoth(stats); }
    }
}