namespace SUI.Client.Core.Infrastructure.CsvParsers;

public static class CsvHeaderValidator
{
    public static void Validate(HashSet<string> headers, IEnumerable<string> requiredHeaders)
    {
        var missingHeaders = GetMissingHeaders(headers, requiredHeaders);

        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"CSV is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }
    }

    public static string[] GetMissingHeaders(
        HashSet<string> headers,
        IEnumerable<string> expectedHeaders
    )
    {
        return expectedHeaders.Where(x => !headers.Contains(x)).ToArray();
    }
}
