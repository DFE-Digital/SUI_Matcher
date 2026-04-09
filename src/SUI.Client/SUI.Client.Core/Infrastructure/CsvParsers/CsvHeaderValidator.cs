namespace SUI.Client.Core.Infrastructure.CsvParsers;

public static class CsvHeaderValidator
{
    public static void Validate(HashSet<string> headers, string parserType)
    {
        var requiredHeaders = parserType switch
        {
            CsvParserNameConstants.TypeOne => new[]
            {
                "GivenName",
                "FamilyName",
                "DOB",
                "Postcode",
            },
            _ => throw new InvalidOperationException($"Unknown parser type: {parserType}."),
        };

        var missingHeaders = requiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"CSV is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }
    }
}
