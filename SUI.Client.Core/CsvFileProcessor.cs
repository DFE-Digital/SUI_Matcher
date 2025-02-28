using CsvHelper;
using CsvHelper.Configuration;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using System.Globalization;

namespace SUI.Client.Core;

public interface ICsvFileProcessor
{
    Task<string> ProcessCsvFileAsync(string filePath, string outputPath);
}

public class CsvFileProcessor(CsvMappingConfig mapping, IMatchPersonApiService matchPersonApi) : ICsvFileProcessor
{
    private readonly CsvMappingConfig _mappingConfig = mapping ?? new();
    private readonly IMatchPersonApiService _matchPersonApi = matchPersonApi;

    public const string HeaderStatus = "SUI_Status";
    public const string HeaderScore = "SUI_Score";
    public const string HeaderNhsNo = "SUI_NHSNo";

    public async Task<string> ProcessCsvFileAsync(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var (headers, records) = await ReadCsvAsync(filePath);

        headers.Add(HeaderStatus);
        headers.Add(HeaderScore);
        headers.Add(HeaderNhsNo);

        foreach (var record in records)
        {
            var payload = new MatchPersonPayload
            {
                Given = record[_mappingConfig.ColumnMappings[nameof(MatchPersonPayload.Given)]],
                Family = record[_mappingConfig.ColumnMappings[nameof(MatchPersonPayload.Family)]],
                BirthDate = record[_mappingConfig.ColumnMappings[nameof(MatchPersonPayload.BirthDate)]],
                Email = record[_mappingConfig.ColumnMappings[nameof(MatchPersonPayload.Email)]],
            };

            var response = await _matchPersonApi.MatchPersonAsync(payload);

            record[HeaderStatus] = response?.Result?.MatchStatus.ToString() ?? "-";
            record[HeaderScore] = response?.Result?.Score.ToString() ?? "-";
            record[HeaderNhsNo] = response?.Result?.NhsNumber ?? "-";
        }


        var outputFilePath = GetOutputFilePath(filePath, outputPath);
        await WriteCsvAsync(outputFilePath, headers, records);

        return outputFilePath;
    }

    private static string GetOutputFilePath(string inputFilePath, string outputPath)
    {
        string filenameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
        string extension = Path.GetExtension(inputFilePath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFilePath = Path.Combine(outputPath, $"{filenameWithoutExt}_output_{timestamp}{extension}");
        return outputFilePath;
    }

    public static async Task<(HashSet<string> Headers, List<Dictionary<string, string>> Records)> ReadCsvAsync(string filePath)
    {
        var headers = new HashSet<string>();
        var records = new List<Dictionary<string, string>>();

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
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
    public static async Task WriteCsvAsync(string outputFilePath, HashSet<string> headers, List<Dictionary<string, string>> records)
    {
        using var writer = new StreamWriter(outputFilePath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        
        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        await csv.NextRecordAsync();


        foreach (var record in records)
        {
            foreach (var header in headers)
            {
                csv.WriteField(record.TryGetValue(header, out string? value) ? value : "");
            }
            await csv.NextRecordAsync();
        }

    }
}
