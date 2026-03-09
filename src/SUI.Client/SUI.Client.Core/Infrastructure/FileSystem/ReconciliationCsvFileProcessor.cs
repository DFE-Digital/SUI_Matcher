using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared;
using Shared.Models;
using Shared.Util;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.UseCases.ReconcilePeople;
using SUI.Client.Core.Infrastructure.Parsing;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public class ReconciliationCsvFileProcessor(
    ILogger<ReconciliationCsvFileProcessor> logger,
    CsvMappingConfig mapping,
    IMatchingService matching,
    IOptions<CsvWatcherConfig> watcherConfig) : ICsvFileProcessor
{
    private readonly ReconciliationCsvProcessStats _stats = new();
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
    public const string HeaderMissingLocalFields = "SUI_MissingLocalFields";
    public const string HeaderMissingNhsFields = "SUI_MissingNhsFields";
    public const string HeaderStatus = "SUI_Status";
    public const string HeaderMatchStatus = "SUI_MatchStatus";
    public const string HeaderMatchNhsNumber = "SUI_MatchNhsNumber";
    public const string HeaderMatchScore = "SUI_MatchScore";
    public const string HeaderMatchProcessStage = "SUI_MatchProcessStage";

    // address comparison
    public const string HeaderPrimaryAddressSame = "SUI_PrimaryAddressSame";
    public const string HeaderAddressHistoriesIntersect = "SUI_AddressHistoriesIntersect";
    public const string HeaderPrimaryCMSAddressInPDSHistory = "SUI_PrimaryCMSAddressInPDSHistory";
    public const string HeaderPrimaryPDSAddressInCMSHistory = "SUI_PrimaryPDSAddressInCMSHistory";

    private async Task ProcessRecord(Dictionary<string, string> record, ReconciliationCsvProcessStats stats)
    {
        string gender = record.GetFirstValueOrDefault(mapping.ColumnMappings[nameof(MatchPersonPayload.Gender)])
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
            StrategyVersion = watcherConfig.Value.StrategyVersion
        };

        var response = await matching.ReconcilePersonAsync(payload);

        var addressComparisonResult = GetAddressComparisonResult(
            payload,
            response,
            record.GetFirstValueOrDefault(mapping.ColumnMappings[CsvMappingConfig.NonRequestFieldsConstants.AddressHistory]));

        record[HeaderNhsNo] = response?.Person?.NhsNumber ?? "-";
        record[HeaderGivenName] = string.Join(" - ", response?.Person?.GivenNames ?? ["-"]);
        record[HeaderFamilyName] = string.Join(" - ", response?.Person?.FamilyNames ?? ["-"]);
        record[HeaderBirthDate] = response?.Person?.BirthDate.ToString() ?? "-";
        record[HeaderGender] = response?.Person?.Gender ?? "-";
        record[HeaderAddressPostalCode] = string.Join(" - ", response?.Person?.AddressPostalCodes ?? ["-"]);
        record[HeaderEmail] = string.Join(" - ", response?.Person?.Emails ?? ["-"]);
        record[HeaderPhone] = string.Join(" - ", response?.Person?.PhoneNumbers ?? ["-"]);
        record[HeaderAddressHistory] = CsvUtils.WrapInputForCsv(response?.Person?.AddressHistory);
        record[HeaderGeneralPractitionerOdsId] = response?.Person?.GeneralPractitionerOdsId ?? "-";
        var differenceList = CreateDelimiterStringFromList(response?.DifferenceFields);
        record[HeaderDifferences] = differenceList;
        record[HeaderMissingLocalFields] = CreateDelimiterStringFromList(response?.MissingLocalFields);
        record[HeaderMissingNhsFields] = CreateDelimiterStringFromList(response?.MissingNhsFields);
        record[HeaderStatus] = response?.Status.ToString() ?? "-";
        record[HeaderMatchNhsNumber] = response?.MatchingResult?.NhsNumber ?? "-";
        record[HeaderMatchStatus] = response?.MatchingResult?.MatchStatus.ToString() ?? "-";
        record[HeaderMatchScore] = response?.MatchingResult?.Score.ToString() ?? "-";
        record[HeaderMatchProcessStage] = response?.MatchingResult?.ProcessStage ?? "-";
        record[HeaderPrimaryAddressSame] = addressComparisonResult.PrimaryAddressSame.ToString();
        record[HeaderAddressHistoriesIntersect] = addressComparisonResult.AddressHistoriesIntersect.ToString();
        record[HeaderPrimaryCMSAddressInPDSHistory] = addressComparisonResult.PrimaryCMSAddressInPDSHistory.ToString();
        record[HeaderPrimaryPDSAddressInCMSHistory] = addressComparisonResult.PrimaryPDSAddressInCMSHistory.ToString();

        stats.RecordMatchStatusStats(response?.MatchingResult?.MatchStatus);
        stats.RecordReconciliationStatusStats(
            response?.Status,
            response?.DifferenceFields.ToArray() ?? [],
            response?.MissingLocalFields.ToArray() ?? [],
            response?.MissingNhsFields.ToArray() ?? []);
        stats.RecordAddressStats(addressComparisonResult);
    }

    private static string CreateDelimiterStringFromList(List<string>? value)
    {
        if (value is null)
        {
            return "-";
        }
        return string.Join(" - ", value);
    }

    private static AddressComparisonResult GetAddressComparisonResult(ReconciliationRequest request, ReconciliationResponse? response, string? addressHistoryCsv)
    {
        var result = new AddressComparisonResult();

        if (response?.Person == null)
        {
            return result;
        }

        var pdsAddressHistory = AddressParser.FromNhsPerson(response.Person);
        var queryingAddressHistory = AddressParser.ParseHistory(addressHistoryCsv);

        result.PrimaryAddressSame = pdsAddressHistory.PrimaryAddressSameAs(queryingAddressHistory);
        result.AddressHistoriesIntersect = pdsAddressHistory.IntersectsWith(queryingAddressHistory);


        result.PrimaryCMSAddressInPDSHistory = queryingAddressHistory.PrimaryAddressInHistoryOf(pdsAddressHistory);
        result.PrimaryPDSAddressInCMSHistory = pdsAddressHistory.PrimaryAddressInHistoryOf(queryingAddressHistory);

        return result;
    }

    private static void AddExtraCsvHeaders(HashSet<string> headers)
    {
        headers.Add(HeaderNhsNo);
        headers.Add(HeaderGivenName);
        headers.Add(HeaderFamilyName);
        headers.Add(HeaderBirthDate);
        headers.Add(HeaderGender);
        headers.Add(HeaderAddressPostalCode);
        headers.Add(HeaderEmail);
        headers.Add(HeaderPhone);
        headers.Add(HeaderAddressHistory);
        headers.Add(HeaderGeneralPractitionerOdsId);
        headers.Add(HeaderDifferences);
        headers.Add(HeaderMissingLocalFields);
        headers.Add(HeaderMissingNhsFields);
        headers.Add(HeaderStatus);
        headers.Add(HeaderMatchStatus);
        headers.Add(HeaderMatchNhsNumber);
        headers.Add(HeaderMatchScore);
        headers.Add(HeaderMatchProcessStage);
        headers.Add(HeaderPrimaryAddressSame);
        headers.Add(HeaderAddressHistoriesIntersect);
        headers.Add(HeaderPrimaryCMSAddressInPDSHistory);
        headers.Add(HeaderPrimaryPDSAddressInCMSHistory);
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

        var outputFilePath = GetOutputFileName(ts, outputDirectory, filePath);
        logger.LogInformation("Writing output CSV file to: {OutputFilePath}", outputFilePath);
        await WriteCsvAsync(outputFilePath, headers, records);


        var statsJsonFileName = WriteStatsJsonFile(outputDirectory, ts, _stats);
        var csvResult = new ProcessCsvFileResult(outputFilePath, statsJsonFileName, _stats, outputDirectory);
        _stats.ResetStats();
        return csvResult;
    }

    public static async Task<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> ReadCsvAsync(
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
    public static async Task<string> WriteCsvAsync(string fileName, HashSet<string> headers,
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

        return fileName;
    }

    private static string GetOutputFileName(string timestamp, string outputDirectory, string fileName)
    {
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(outputDirectory, $"{filenameWithoutExt}_output_{timestamp}{extension}");
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