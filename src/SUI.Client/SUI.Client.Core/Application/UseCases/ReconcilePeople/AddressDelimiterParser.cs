using System.Text.RegularExpressions;

using Shared.Models;

using SUI.Client.Core.Domain.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

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

        // Extract line 1 - assumes address line 1
        var addressLine1 = parts[1];
        var addressLine1Number = ExtractHouseNumber(addressLine1);

        // Extract line 2 - assumes address line 2
        var addressLine2 = parts[2];
        var addressLine2Number = ExtractHouseNumber(addressLine2);

        var postcode = NormalizePostcode(parts[^1]);

        if (string.IsNullOrWhiteSpace(postcode))
            return null;

        return new AddressMinimal(addressLine1Number, addressLine2Number, postcode);
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

        if (!TryGetNumberFromAddressLine(addressLine, out var candidate))
        {
            return null;
        }

        return candidate;
    }

    private static bool TryGetNumberFromAddressLine(string addressLine, out string? number)
    {
        number = null;

        if (string.IsNullOrWhiteSpace(addressLine))
            return false;

        var value = addressLine.Trim();
        var match = LeadingNumberRegex.Match(value);

        if (!match.Success)
            return false;

        number = match.Groups[1].Value.Replace(" ", "").ToUpperInvariant();
        return true;
    }

    private static string NormalizePostcode(string postcode)
    {
        return new string(postcode
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();
    }

    // "house number" or "street address" parser.
    // It’s designed to handle both standard numbers
    // and those slightly more complex variations you see,
    // like ranges or numbers with letters attached.
    private static readonly Regex LeadingNumberRegex =
        new(@"^(\d+\s*-\s*\d+|\d+[A-Za-z]?)\b", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
}