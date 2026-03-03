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
}