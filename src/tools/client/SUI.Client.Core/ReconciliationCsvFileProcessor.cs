using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using Shared.Models;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public class ReconciliationCsvFileProcessor(
    ILogger<ReconciliationCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchPersonApiService matchPersonApi) : CsvFileProcessorBase(logger, new ReconciliationCsvProcessStats()), ICsvFileProcessor
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
            "Birthdate differences", "Birthdate missing NHS", "Birthdate missing LA", "Birthdate Missing Both",
            "Email differences", "Email missing NHS", "Email missing LA", "Email Missing Both",
            "Phone differences", "Phone missing NHS", "Phone missing LA", "Phone Missing Both",
            "Given Name differences", "Given Name missing NHS", "Given Name missing LA", "Given Name Missing Both",
            "Family Name differences", "Family Name missing NHS", "Family Name missing LA", "Family Name Missing Both",
            "Postcode differences", "Postcode missing NHS", "Postcode missing LA", "Postcode Missing Both",
        ];
        double[] values =
        [
            localStats.ErroredCount, localStats.NoDifferenceCount, localStats.SupersededNhsNumber,
            localStats.InvalidNhsNumber, localStats.PatientNotFound, localStats.MissingNhsNumber, localStats.DifferencesCount,
            localStats.BirthDateCount, localStats.BirthDateNhsCount, localStats.BirthDateLaCount, localStats.BirthDateBothCount,
            localStats.EmailCount, localStats.EmailNhsCount, localStats.EmailLaCount, localStats.EmailBothCount,
            localStats.PhoneCount, localStats.PhoneNhsCount, localStats.PhoneLaCount, localStats.PhoneBothCount,
            localStats.GivenNameCount, localStats.GivenNameNhsCount, localStats.GivenNameLaCount, localStats.GivenNameBothCount,
            localStats.FamilyNameCount, localStats.FamilyNameNhsCount, localStats.FamilyNameLaCount, localStats.FamilyNameBothCount,
            localStats.PostCodeCount, localStats.PostCodeNhsCount, localStats.PostCodeLaCount, localStats.PostCodeBothCount
        ];
        return PdfReportGenerator.GenerateReport(GetOutputFileName(ts, outputDirectory, "ReconciliationReport.pdf"), "Reconciliation Report", categories, values);
    }

    private static void RecordStats(ReconciliationCsvProcessStats stats, ReconciliationResponse? response, string differenceList)
    {
        stats.Count++;
        switch (response?.Status)
        {
            case ReconciliationStatus.NoDifferences:
                stats.NoDifferenceCount++;
                break;
            case ReconciliationStatus.Differences:
                if (Regex.IsMatch(differenceList, @"\bBirthDate\b(?!:)")) { stats.BirthDateCount++; }
                if (differenceList.Contains("BirthDate:NHS")) { stats.BirthDateNhsCount++; }
                if (differenceList.Contains("BirthDate:LA")) { stats.BirthDateLaCount++; }
                if (differenceList.Contains("BirthDate:Both")) { stats.BirthDateBothCount++; }
                if (Regex.IsMatch(differenceList, @"\bEmail\b(?!:)")) { stats.EmailCount++; }
                if (differenceList.Contains("Email:NHS")) { stats.EmailNhsCount++; }
                if (differenceList.Contains("Email:LA")) { stats.EmailLaCount++; }
                if (differenceList.Contains("Email:Both")) { stats.EmailBothCount++; }
                if (Regex.IsMatch(differenceList, @"\bPhone\b(?!:)")) { stats.PhoneCount++; }
                if (differenceList.Contains("Phone:NHS")) { stats.PhoneNhsCount++; }
                if (differenceList.Contains("Phone:LA")) { stats.PhoneLaCount++; }
                if (differenceList.Contains("Phone:Both")) { stats.PhoneBothCount++; }
                if (Regex.IsMatch(differenceList, @"\bGiven\b(?!:)")) { stats.GivenNameCount++; }
                if (differenceList.Contains("Given:NHS")) { stats.GivenNameNhsCount++; }
                if (differenceList.Contains("Given:LA")) { stats.GivenNameLaCount++; }
                if (differenceList.Contains("Given:Both")) { stats.GivenNameBothCount++; }
                if (Regex.IsMatch(differenceList, @"\bFamily\b(?!:)")) { stats.FamilyNameCount++; }
                if (differenceList.Contains("Family:NHS")) { stats.FamilyNameNhsCount++; }
                if (differenceList.Contains("Family:LA")) { stats.FamilyNameLaCount++; }
                if (differenceList.Contains("Family:Both")) { stats.FamilyNameBothCount++; }
                if (Regex.IsMatch(differenceList, @"\bAddressPostalCode\b(?!:)")) { stats.PostCodeCount++; }
                if (differenceList.Contains("AddressPostalCode:NHS")) { stats.PostCodeNhsCount++; }
                if (differenceList.Contains("AddressPostalCode:LA")) { stats.PostCodeLaCount++; }
                if (differenceList.Contains("AddressPostalCode:Both")) { stats.PostCodeBothCount++; }
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
}