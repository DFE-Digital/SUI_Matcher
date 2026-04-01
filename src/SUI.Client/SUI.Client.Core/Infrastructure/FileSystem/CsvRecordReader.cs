using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public static class CsvRecordReader
{
    public static async Task<(
        HashSet<string> Headers,
        List<Dictionary<string, string>> Records
    )> ReadCsvFileAsync(string filePath)
    {
        if (!await IsFileReadyAsync(filePath))
        {
            throw new IOException($"File {filePath} is not ready for reading.");
        }

        using var reader = new StreamReader(filePath);
        return await ReadCsvTextAsync(reader);
    }

    public static async Task<(
        HashSet<string> Headers,
        List<Dictionary<string, string>> Records
    )> ReadCsvTextAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var records = new List<Dictionary<string, string>>();

        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                MissingFieldFound = null,
                HeaderValidated = null,
            }
        );

        if (!await csv.ReadAsync())
        {
            return (headers, records);
        }

        csv.ReadHeader();

        if (csv.HeaderRecord is not null)
        {
            headers.UnionWith(csv.HeaderRecord.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = (csv.GetField(header) ?? string.Empty).Trim();
            }

            records.Add(row);
        }

        return (headers, records);
    }

    private static async Task<bool> IsFileReadyAsync(
        string filePath,
        int maxAttempts = 5,
        int delayMs = 1000
    )
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None
                );
                return true;
            }
            catch (IOException)
            {
                await Task.Delay(delayMs);
            }
        }

        return false;
    }
}
