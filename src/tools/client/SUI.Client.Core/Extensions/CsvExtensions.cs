namespace SUI.Client.Core.Extensions;

public static class CsvExtensions
{
    public static string GetFirstValueOrDefault(this Dictionary<string, string> record, IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            if (record.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        return string.Empty;
    }
}