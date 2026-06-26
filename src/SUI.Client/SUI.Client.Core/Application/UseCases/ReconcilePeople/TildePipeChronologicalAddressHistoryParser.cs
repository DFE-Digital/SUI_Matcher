using SUI.Client.Core.Domain.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public sealed class TildePipeChronologicalAddressHistoryParser : ISourceAddressHistoryParser
{
    private const string MultipleAddressDelimiter = "|";

    public AddressHistory Parse(string? historyString, string? primaryPostcode)
    {
        if (string.IsNullOrWhiteSpace(historyString) || string.IsNullOrWhiteSpace(primaryPostcode))
        {
            return new AddressHistory([]);
        }

        var parts = historyString.Split(
            MultipleAddressDelimiter,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        var addresses = parts.Select(AddressParser.ParseRecord).OfType<AddressMinimal>().ToList();

        // Primary address is identified by matching the provided primary postcode to the postcode of the parsed addresses and selecting the latest.
        // Addresses 'should' be in chronological order, but we will select the last match as a fallback in case of duplicates or ordering issues.
        var normalizedPostcode = AddressParser.NormalizePostcode(primaryPostcode);
        AddressMinimal? primaryAddress = addresses.LastOrDefault(a =>
            a.Postcode.Equals(normalizedPostcode, StringComparison.OrdinalIgnoreCase)
        );

        return new AddressHistory(addresses, primaryAddress);
    }
}
