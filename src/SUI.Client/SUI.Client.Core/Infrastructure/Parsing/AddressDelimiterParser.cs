using SUI.Client.Core.Application.Models;

namespace SUI.Client.Core.Infrastructure.Parsing;

public static class AddressParser
{
    /// <summary>
    /// Parses an address record delimited by "~" into its components, specifically extracting the house number and postcode.
    /// </summary>
    public static AddressMinimal? ParseRecord(string record)
    {
        if (string.IsNullOrWhiteSpace(record))
            return null;

        var parts = record.Split('~', StringSplitOptions.TrimEntries);

        // Expect at least: id ~ address line ~ postcode
        if (parts.Length < 3)
            return null;

        var addressLine = parts[1];
        var houseNumber = ExtractHouseNumber(addressLine);

        if (houseNumber is null)
            return null;

        var postcode = NormalizePostcode(parts[^1]);

        if (string.IsNullOrWhiteSpace(postcode))
            return null;

        return new AddressMinimal(houseNumber, postcode);
    }

    private static string? ExtractHouseNumber(string addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return null;

        // House number is the first word in the address line
        var tokens = addressLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return null;

        var candidate = tokens[0].ToUpperInvariant();

        return IsValidHouseNumber(candidate) ? candidate : null;
    }

    private static bool IsValidHouseNumber(string value)
    {
        // Accept formats like "12" or "12A". Must start with a digit and can be followed by letters.
        if (!char.IsDigit(value[0]))
            return false;

        if (value.Length == 1)
            return true;

        for (int i = 1; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]) && !char.IsLetter(value[i]))
                return false;
        }

        return true;
    }

    private static string NormalizePostcode(string postcode)
    {
        return postcode
            .Trim()
            .Replace(" ", "")
            .ToUpperInvariant();
    }
}