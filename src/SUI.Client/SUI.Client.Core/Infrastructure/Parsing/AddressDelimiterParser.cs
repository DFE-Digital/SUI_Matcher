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
    /// </summary>
    public static AddressHistory ParseHistory(string? historyString, string? primaryPostcode = null)
    {
        if (string.IsNullOrWhiteSpace(historyString))
        {
            return new AddressHistory([]);
        }

        var parts = historyString.Split(MultipleAddressDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var addresses = parts
            .Select(ParseRecord)
            .Where(a => a != null)
            .Cast<AddressMinimal>()
            .ToList();

        AddressMinimal? primaryAddress = null;
        if (!string.IsNullOrWhiteSpace(primaryPostcode))
        {
            var normalizedPrimary = NormalizePostcode(primaryPostcode);
            // Search for the primary address in the parsed history
            primaryAddress = addresses.FirstOrDefault(a => a.Postcode.Equals(normalizedPrimary, StringComparison.OrdinalIgnoreCase));
        }

        return new AddressHistory(addresses, primaryAddress);
    }

    /// <summary>
    /// Creates an AddressHistory from an NhsPerson's address history.
    /// </summary>
    public static AddressHistory FromNhsPerson(NhsPerson person)
    {
        var addresses = person.AddressHistory
            .Select(ParseRecord)
            .Where(a => a != null)
            .Cast<AddressMinimal>()
            .ToList();

        var primaryPostcode = person.AddressPostalCodes.FirstOrDefault();
        AddressMinimal? primaryAddress = null;

        if (!string.IsNullOrWhiteSpace(primaryPostcode))
        {
            var normalizedPrimary = NormalizePostcode(primaryPostcode);
            primaryAddress = addresses.FirstOrDefault(a => a.Postcode.Equals(normalizedPrimary, StringComparison.OrdinalIgnoreCase));
        }

        return new AddressHistory(addresses, primaryAddress);
    }

    /// <summary>
    /// Creates an AddressHistory from a primary postcode. 
    /// Note: House number is not available here, so we might need to handle this.
    /// However, in the CSV processor, we have the address line.
    /// </summary>
    public static AddressHistory FromPrimaryAddress(string? addressLine, string? postcode)
    {
        if (string.IsNullOrWhiteSpace(addressLine) || string.IsNullOrWhiteSpace(postcode))
        {
            return new AddressHistory([]);
        }

        var houseNumber = ExtractHouseNumber(addressLine);
        if (houseNumber == null)
        {
            return new AddressHistory([]);
        }

        var address = new AddressMinimal(houseNumber, NormalizePostcode(postcode));
        return new AddressHistory(new[] { address }, address);
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