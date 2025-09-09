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
        record[HeaderStatus] = response?.Status.ToString() ?? "-";

        RecordStats((ReconciliationCsvProcessStats)stats, response);
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
        headers.Add(HeaderStatus);
    }

    protected override Task CreateMatchedCsvIfEnabled(string filePath, string ts, List<Dictionary<string, string>> records, HashSet<string> headers)
    {
        return Task.CompletedTask;
    }

    protected override string GeneratePdfReport(IStats stats, string ts, string outputDirectory)
    {
        var localStats = (ReconciliationCsvProcessStats)stats;
        string[] categories = ["Errored", "No Differences", "One Difference", "Many Differences", "Superseded NHS Number"];
        double[] values = [localStats.ErroredCount, localStats.NoDifferenceCount, localStats.OneDifferenceCount, localStats.ManyDifferencesCount, localStats.SupersededNhsNumber];
        return PdfReportGenerator.GenerateReport(GetOutputFileName(ts, outputDirectory, "ReconciliationReport.pdf"), "Reconciliation Report", categories, values);
    }

    private static void RecordStats(ReconciliationCsvProcessStats stats, ReconciliationResponse? response)
    {
        stats.Count++;
        switch (response?.Status)
        {
            case ReconciliationStatus.NoDifferences:
                stats.NoDifferenceCount++;
                break;

            case ReconciliationStatus.ManyDifferences:
                stats.ManyDifferencesCount++;
                break;

            case ReconciliationStatus.SupersededNhsNumber:
                stats.SupersededNhsNumber++;
                break;

            case ReconciliationStatus.OneDifference:
                stats.OneDifferenceCount++;
                break;
            case ReconciliationStatus.Error:
                stats.ErroredCount++;
                break;
        }
    }
}