using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

namespace E2E.Tests.Client;

public static class CsvFile
{
    public static (HashSet<string> Headers, List<Dictionary<string, string>> Records) GetData(string filePath)
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
            csv.Read();
            csv.ReadHeader();

            if (csv.HeaderRecord is not null)
            {
                headers.UnionWith(csv.HeaderRecord);
            }

            while (csv.Read())
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
}