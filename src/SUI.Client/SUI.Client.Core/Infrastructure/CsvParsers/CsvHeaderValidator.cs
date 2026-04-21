namespace SUI.Client.Core.Infrastructure.CsvParsers;

public static class CsvHeaderValidator
{
    public static void Validate(HashSet<string> headers, IEnumerable<string> requiredHeaders)
    {
        var missingHeaders = requiredHeaders.Where(x => !headers.Contains(x)).ToArray();

        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"CSV is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }
    }
}
