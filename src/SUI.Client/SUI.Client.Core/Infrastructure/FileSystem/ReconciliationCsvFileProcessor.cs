using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared;
using Shared.Models;
using Shared.Util;

using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public class ReconciliationCsvFileProcessor(
    ILogger<ReconciliationCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchingService matching,
    IOptions<CsvWatcherConfig> watcherConfig) : ICsvFileProcessor
{
    private readonly IStats _stats = new ReconciliationCsvProcessStats();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    public const string HeaderNhsNo = "SUI_NHSNo";
    public const string HeaderGivenName = "SUI_GivenName";
    public const string HeaderAddressPostalCode = "SUI_PostalCode";
    public const string HeaderFamilyName = "SUI_FamilyName";
    public const string HeaderBirthDate = "SUI_BirthDate";
    public const string HeaderGender = "SUI_Gender";
    public const string HeaderEmail = "SUI_Email";
    public const string HeaderPhone = "SUI_Phone";
    public const string HeaderAddressHistory = "SUI_AddressHistory";
    public const string HeaderGeneralPractitionerOdsId = "SUI_GeneralPractitionerOdsId";
    public const string HeaderDifferences = "SUI_Differences";
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderMatchStatus = "SUI_MatchStatus";
    public const string HeaderMatchNhsNumber = "SUI_MatchNhsNumber";
    public const string HeaderMatchScore = "SUI_MatchScore";
    public const string HeaderMatchProcessStage = "SUI_MatchProcessStage";

    private async Task ProcessRecord(DataRow row, IStats stats)
    {
        string gender = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)])
            .ToLower();

        if (int.TryParse(gender, out int _))
        {
            var genderFromNumber = PersonSpecificationUtils.ToGenderFromNumber(gender);
            gender = genderFromNumber;
            // Update the record with the string representation
            row[nameof(ReconciliationRequest.Gender)] = genderFromNumber;
        }

        var dob = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.BirthDate)]);
        ReconciliationRequest payload = new()
        {
            NhsNumber = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.NhsNumber)]),
            Given = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Given)]),
            Family = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Family)]),
            BirthDate =
                dob.ToDateOnly([Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish]),
            Email = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Email)]),
            AddressPostalCode =
                row.GetFirstValueOrDefault(
                    mapping.ColumnMappings[nameof(ReconciliationRequest.AddressPostalCode)]),
            Gender = gender,
            Phone = row.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(ReconciliationRequest.Phone)]),
            SearchStrategy = watcherConfig.Value.SearchStrategy ?? SharedConstants.SearchStrategy.Strategies.Strategy1,
        };

        var response = await matching.ReconcilePersonAsync(payload);

        row[HeaderNhsNo] = response?.Person?.NhsNumber ?? "-";
        row[HeaderGivenName] = string.Join(" - ", response?.Person?.GivenNames ?? ["-"]);
        row[HeaderFamilyName] = string.Join(" - ", response?.Person?.FamilyNames ?? ["-"]);
        row[HeaderBirthDate] = response?.Person?.BirthDate.ToString() ?? "-";
        row[HeaderGender] = response?.Person?.Gender ?? "-";
        row[HeaderAddressPostalCode] = string.Join(" - ", response?.Person?.AddressPostalCodes ?? ["-"]);
        row[HeaderEmail] = string.Join(" - ", response?.Person?.Emails ?? ["-"]);
        row[HeaderPhone] = string.Join(" - ", response?.Person?.PhoneNumbers ?? ["-"]);
        row[HeaderAddressHistory] = CsvUtils.WrapInputForCsv(response?.Person?.AddressHistory);
        row[HeaderGeneralPractitionerOdsId] = response?.Person?.GeneralPractitionerOdsId ?? "-";
        var differenceList = response?.DifferenceString ?? "-";
        row[HeaderDifferences] = differenceList;
        row[HeaderStatus] = response?.Status.ToString() ?? "-";
        row[HeaderMatchNhsNumber] = response?.MatchingResult?.NhsNumber ?? "-";
        row[HeaderMatchStatus] = response?.MatchingResult?.MatchStatus.ToString() ?? "-";
        row[HeaderMatchScore] = response?.MatchingResult?.Score.ToString() ?? "-";
        row[HeaderMatchProcessStage] = response?.MatchingResult?.ProcessStage ?? "-";

        RecordStats((ReconciliationCsvProcessStats)stats, response, differenceList);
    }

    private void AddExtraCsvHeaders(DataTable inputData)
    {
        inputData.Columns.Add(HeaderNhsNo);
        inputData.Columns.Add(HeaderGivenName);
        inputData.Columns.Add(HeaderFamilyName);
        inputData.Columns.Add(HeaderBirthDate);
        inputData.Columns.Add(HeaderGender);
        inputData.Columns.Add(HeaderAddressPostalCode);
        inputData.Columns.Add(HeaderEmail);
        inputData.Columns.Add(HeaderPhone);
        inputData.Columns.Add(HeaderAddressHistory);
        inputData.Columns.Add(HeaderGeneralPractitionerOdsId);
        inputData.Columns.Add(HeaderDifferences);
        inputData.Columns.Add(HeaderStatus);
        inputData.Columns.Add(HeaderMatchStatus);
        inputData.Columns.Add(HeaderMatchNhsNumber);
        inputData.Columns.Add(HeaderMatchScore);
        inputData.Columns.Add(HeaderMatchProcessStage);
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
            case MatchStatus.LowConfidenceMatch:
                stats.MatchingStatusLowConfidenceMatch++;
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
                UpdateStatsForField(differenceList, stats, "MatchingNhsNumber", s => s.MatchingNhsNumberCount++, s => s.MatchingNhsNumberNhsCount++, s => s.MatchingNhsNumberLaCount++, s => s.MatchingNhsNumberBothCount++);

                stats.DifferencesCount++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsSuperseded:
                stats.SupersededNhsNumber++;
                break;
            case ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber:
                stats.MissingNhsNumber++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsNotFoundInNhs:
                stats.PatientNotFound++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsNotValid:
                stats.InvalidNhsNumber++;
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
        var plainRegex = new Regex($@"\b{fieldName}\b(?!:)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(300));

        if (plainRegex.IsMatch(differenceList)) { incrementPlain(stats); }
        if (differenceList.Contains($"{fieldName}:NHS")) { incrementNhs(stats); }
        if (differenceList.Contains($"{fieldName}:LA")) { incrementLa(stats); }
        if (differenceList.Contains($"{fieldName}:Both")) { incrementBoth(stats); }
    }

    public async Task<ProcessCsvFileResult> ProcessCsvFileAsync(DataTable inputData, string outputPath)
    {
        AddExtraCsvHeaders(inputData);

        int totalRecords = inputData.Rows.Count;
        int currentRecord = 0;
        var progressStopwatch = new Stopwatch();
        progressStopwatch.Start();

        logger.LogInformation("Beginning to process {TotalRecords} records from: {TableName}", totalRecords,
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