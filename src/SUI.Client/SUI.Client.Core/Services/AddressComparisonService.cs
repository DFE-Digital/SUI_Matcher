using SUI.Client.Core.Infrastructure.Parsing;

namespace SUI.Client.Core.Services;

public class AddressComparisonService
{
    public static bool ContainsPostcode(string address, string postcode)
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(postcode))
        {
            return false;
        }

        return address.Contains(postcode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///  Determines if two address strings have any intersection in their history of house numbers and postcodes.
    /// Single address delimiter is currently "~" and multiple addresses are delimited by "|".
    /// This is based on the current client parsing logic and may need to be updated if that changes.
    /// </summary>
    /// <param name="address1"></param>
    /// <param name="address2"></param>
    /// <returns></returns>
    public static bool AddressesHistoryIntersect(string address1, string address2)
    {
        // This could change based on how it's parsed in the client tool, but stable for now.
        const string multipleAddressDelimiter = "|";
        
        if (string.IsNullOrWhiteSpace(address1) || string.IsNullOrWhiteSpace(address2))
        {
            return false;
        }
        
        var address1Parts = address1.Split(multipleAddressDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var address2Parts = address2.Split(multipleAddressDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var address1Minimal = address1Parts.Select(AddressParser.ParseRecord);
        var address2Minimal = address2Parts.Select(AddressParser.ParseRecord);
        
        // Does address 1 contain anything from address two with house number and postcode and vice versa?
        return address1Minimal.Any(a1 => a1 != null && address2Minimal
            .Any(a2 => a2 != null && a1.HouseNumber.
                Equals(a2.HouseNumber, StringComparison.OrdinalIgnoreCase) 
                & a1.Postcode.Equals(a2.Postcode, StringComparison.OrdinalIgnoreCase)));
    }
}
