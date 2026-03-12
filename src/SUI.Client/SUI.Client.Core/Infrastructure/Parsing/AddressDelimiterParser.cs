using Shared.Models;

using SUI.Client.Core.Domain.Models;

namespace SUI.Client.Core.Infrastructure.Parsing;

public static class AddressParser
{
    private const string MultipleAddressDelimiter = "|";

    /// <summary>
    /// Parses an address record delimited by "~" into its components, specifically extracting the house number and postcode.
    /// </summary>
    public static AddressMinimal? ParseRecord(string? record)
    {
        if (string.IsNullOrWhiteSpace(record))
            return null;

        var parts = record.Split('~', StringSplitOptions.TrimEntries);

        // Expect at least: id ~ address line ~ postcode
        if (parts.Length < 3)
            return null;

        // Constraint that house number is always the first word in the second segment of the record
        var addressLine = parts[1];
        var houseNumber = ExtractHouseNumber(addressLine);

        if (houseNumber is null)
            return null;

        var postcode = NormalizePostcode(parts[^1]);

        if (string.IsNullOrWhiteSpace(postcode))
            return null;

        return new AddressMinimal(houseNumber, postcode);
    }

    /// <summary>
    /// Parses a history of address records delimited by "|" into an AddressHistory object.
    /// Each record in the history is delimited by "~".
    /// <para>The order of the history string is preserved</para>
    /// </summary>
    public static AddressHistory ParseHistory(string? historyString, string? primaryPostcode)
    {
        if (string.IsNullOrWhiteSpace(historyString) || string.IsNullOrWhiteSpace(primaryPostcode))
        {
            return new AddressHistory([]);
        }

        var parts = historyString.Split(MultipleAddressDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var addresses = ParseToAddressMinimalList(parts);

        // Primary address is identified by matching the provided primary postcode to the postcode of the parsed addresses and selecting the latest.
        // Addresses 'should' be in chronological order, but we will select the last match as a fallback in case of duplicates or ordering issues.
        var normalizedPostcode = NormalizePostcode(primaryPostcode);
        AddressMinimal? primaryAddress = addresses.LastOrDefault(a => a.Postcode.Equals(normalizedPostcode, StringComparison.OrdinalIgnoreCase));

        return new AddressHistory(addresses, primaryAddress);
    }

    /// <summary>
    /// Creates an AddressHistory from an NhsPerson's address history.
    /// </summary>
    public static AddressHistory FromNhsPerson(NhsPerson person)
    {
        var addresses = ParseToAddressMinimalList(person.AddressHistory);

        var primaryPostcode = person.AddressPostalCodes.FirstOrDefault();

        // Assuming PDS is consistent in providing the primary address's postcode as the first entry in AddressPostalCodes,
        // we can attempt to match it to one of the parsed addresses to identify the primary address.
        if (string.IsNullOrWhiteSpace(primaryPostcode))
        {
            return new AddressHistory(addresses);
        }

        var normalizedPostcode = NormalizePostcode(primaryPostcode);
        AddressMinimal? primaryAddress = addresses.LastOrDefault(a => a.Postcode.Equals(normalizedPostcode, StringComparison.OrdinalIgnoreCase));

        return new AddressHistory(addresses, primaryAddress);
    }

    private static List<AddressMinimal> ParseToAddressMinimalList(string[] parts)
    {
        return parts
            .Select(ParseRecord)
            .OfType<AddressMinimal>()
            .ToList();
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
        return new string(postcode
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();
    }
}