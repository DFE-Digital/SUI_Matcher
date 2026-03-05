namespace SUI.Client.Core.Domain.Models;

public class AddressHistory(IEnumerable<AddressMinimal> addresses, AddressMinimal? primaryAddress = null)
{
    private readonly List<AddressMinimal> _addresses = addresses.ToList();

    public IReadOnlyList<AddressMinimal> Addresses => _addresses;

    public AddressMinimal? PrimaryAddress { get; } = primaryAddress;


    public bool PrimaryAddressSameAs(AddressHistory? other)
    {
        if (PrimaryAddress == null || other?.PrimaryAddress == null)
        {
            return false;
        }

        return AreAddressesEqual(PrimaryAddress, other.PrimaryAddress);
    }

    public bool IntersectsWith(AddressHistory? other)
    {
        if (other == null)
        {
            return false;
        }

        return _addresses.Any(a1 => other.Addresses.Any(a2 => AreAddressesEqual(a1, a2)));
    }

    public bool PrimaryAddressInHistoryOf(AddressHistory? other)
    {
        if (PrimaryAddress == null || other == null)
        {
            return false;
        }

        return other.Addresses.Any(a => AreAddressesEqual(PrimaryAddress, a));
    }

    public bool ContainsPostcode(string? postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode))
        {
            return false;
        }

        var normalized = postcode.Trim().Replace(" ", "").ToUpperInvariant();
        return _addresses.Any(a => a.Postcode.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static bool AreAddressesEqual(AddressMinimal a1, AddressMinimal a2)
    {
        return a1.HouseNumber.Equals(a2.HouseNumber, StringComparison.OrdinalIgnoreCase) &&
               a1.Postcode.Equals(a2.Postcode, StringComparison.OrdinalIgnoreCase);
    }
}
