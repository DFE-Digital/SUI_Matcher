using SUI.Client.Core.Domain.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public sealed class SemicolonCommaNewestFirstAddressHistoryParser : ISourceAddressHistoryParser
{
    public AddressHistory Parse(string? historyString, string? primaryPostcode)
    {
        if (string.IsNullOrWhiteSpace(historyString) || string.IsNullOrWhiteSpace(primaryPostcode))
        {
            return new AddressHistory([]);
        }

        var addresses = historyString
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseCommaSeparatedAddress)
            .OfType<AddressMinimal>()
            .ToList();
        var normalizedPostcode = AddressParser.NormalizePostcode(primaryPostcode);
        var primaryAddress = addresses.FirstOrDefault(address =>
            address.Postcode.Equals(normalizedPostcode, StringComparison.OrdinalIgnoreCase)
        );

        return new AddressHistory(addresses, primaryAddress);
    }

    private static AddressMinimal? ParseCommaSeparatedAddress(string value)
    {
        var parts = value.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        if (parts.Length < 2)
        {
            return null;
        }

        var postcode = AddressParser.NormalizePostcode(parts[^1]);
        if (string.IsNullOrWhiteSpace(postcode))
        {
            return null;
        }

        return new AddressMinimal(
            AddressParser.ExtractHouseNumber(parts[0]),
            parts.Length > 2 ? AddressParser.ExtractHouseNumber(parts[1]) : null,
            postcode
        );
    }
}
