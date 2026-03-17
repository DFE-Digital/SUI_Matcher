using SUI.Client.Core.Application.UseCases.ReconcilePeople;

namespace SUI.Client.Core.Domain.Models;

public class AddressHistory(IEnumerable<AddressMinimal> addresses, AddressMinimal? primaryAddress = null)
{
    private readonly List<AddressMinimal> _addresses = addresses.ToList();

    public IReadOnlyList<AddressMinimal> Addresses => _addresses;

    public AddressMinimal? PrimaryAddress { get; } = primaryAddress;


    public AddressComparisonResult PrimaryAddressSameAs(AddressHistory? other)
    {
        if (PrimaryAddress == null || other?.PrimaryAddress == null)
        {
            return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
        }

        return AreAddressesEqual(PrimaryAddress, other.PrimaryAddress);
    }

    public AddressComparisonResult IntersectsWith(AddressHistory? other)
    {
        if (other == null)
        {
            return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
        }

        // Check all combinations and return the first match or uncertain result
        foreach (var a1 in _addresses)
        {
            foreach (var a2 in other.Addresses)
            {
                var result = AreAddressesEqual(a1, a2);
                if (result.Status is AddressComparisonResult.AddressMatchStatus.Matched or AddressComparisonResult.AddressMatchStatus.Uncertain)
                {
                    return result;
                }
            }
        }

        return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
    }

    public AddressComparisonResult PrimaryAddressInHistoryOf(AddressHistory? other)
    {
        if (PrimaryAddress == null || other == null)
        {
            return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
        }

        // Check primary address against all addresses in other's history
        foreach (var address in other.Addresses)
        {
            var result = AreAddressesEqual(PrimaryAddress, address);
            if (result.Status == AddressComparisonResult.AddressMatchStatus.Matched ||
                result.Status == AddressComparisonResult.AddressMatchStatus.Uncertain)
            {
                return result;
            }
        }

        return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
    }

    private static AddressComparisonResult AreAddressesEqual(AddressMinimal a1, AddressMinimal a2)
    {
        // Rule 1: If postcodes don't match, return unmatched
        if (!a1.Postcode.Equals(a2.Postcode, StringComparison.OrdinalIgnoreCase))
        {
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Unmatched, 
                AddressComparisonResult.AddressMatchReason.PostcodeMismatch);
        }
        
        // Rule 2: Null checks - if lines 1 and 2 are null on both a1 and a2 then its unmatched
        if(string.IsNullOrWhiteSpace(a1.AddressLineOne) && string.IsNullOrWhiteSpace(a1.AddressLineTwo) &&
           string.IsNullOrWhiteSpace(a2.AddressLineOne) && string.IsNullOrWhiteSpace(a2.AddressLineTwo))
        {
            return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
        }
        
        // Rule 3: Do address line one match exactly? = Match
        if (!string.IsNullOrEmpty(a1.AddressLineOne) && 
            a1.AddressLineOne.Equals(a2.AddressLineOne, StringComparison.OrdinalIgnoreCase))
        {
            return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Matched);
        }

        // Extract building numbers from both lines
        var a1Line1Number = ExtractBuildingNumber(a1.AddressLineOne);
        var a2Line1Number = ExtractBuildingNumber(a2.AddressLineOne);
        var a1Line2Number = ExtractBuildingNumber(a1.AddressLineTwo);
        var a2Line2Number = ExtractBuildingNumber(a2.AddressLineTwo);

        // Rule 4: If Line1 has no numbers on both addresses, compare Line2 numbers = Match
        if (a1Line1Number == null && a2Line1Number == null)
        {
            // Both Line2 have numbers and they match
            if (a1Line2Number != null && a2Line2Number != null && 
                a1Line2Number.Equals(a2Line2Number, StringComparison.OrdinalIgnoreCase))
            {
                return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Matched);
            }
            
            // Neither Line1 nor Line2 have numbers - can't compare
            if (a1Line2Number == null && a2Line2Number == null)
            {
                return new AddressComparisonResult(
                    AddressComparisonResult.AddressMatchStatus.Unmatched,
                    AddressComparisonResult.AddressMatchReason.BuildingNumberMissing);
            }
            
            // One has Line2 number, one doesn't
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Unmatched,
                AddressComparisonResult.AddressMatchReason.BuildingNumberMissing);
        }

        // Determine which numbers to use for comparison (prefer Line1, fallback to Line2)
        var a1Number = a1Line1Number ?? a1Line2Number;
        var a2Number = a2Line1Number ?? a2Line2Number;

        // If we still don't have building numbers to compare, we can't proceed
        if (a1Number == null || a2Number == null)
        {
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Unmatched,
                AddressComparisonResult.AddressMatchReason.BuildingNumberMissing);
        }

        // Rule 5: Flat/apartment detection - if one address has numbers in both lines and the other doesn't
        // This indicates potential flat/unit scenario where flat and building are on separate lines
        var a1HasBothLineNumbers = a1Line1Number != null && a1Line2Number != null;
        var a2HasBothLineNumbers = a2Line1Number != null && a2Line2Number != null;
        
        if (a1HasBothLineNumbers && !a2HasBothLineNumbers)
        {
            // Check if a2's number appears in either of a1's lines
            if ((a1Line1Number != null && a1Line1Number.Equals(a2Number, StringComparison.OrdinalIgnoreCase)) ||
                (a1Line2Number != null && a1Line2Number.Equals(a2Number, StringComparison.OrdinalIgnoreCase)))
            {
                return new AddressComparisonResult(
                    AddressComparisonResult.AddressMatchStatus.Uncertain,
                    AddressComparisonResult.AddressMatchReason.FlatMissing);
            }
        }
        
        if (a2HasBothLineNumbers && !a1HasBothLineNumbers)
        {
            // Check if a1's number appears in either of a2's lines
            if ((a2Line1Number != null && a2Line1Number.Equals(a1Number, StringComparison.OrdinalIgnoreCase)) ||
                (a2Line2Number != null && a2Line2Number.Equals(a1Number, StringComparison.OrdinalIgnoreCase)))
            {
                return new AddressComparisonResult(
                    AddressComparisonResult.AddressMatchStatus.Uncertain,
                    AddressComparisonResult.AddressMatchReason.FlatMissing);
            }
        }

        // Rule 6: Does one address's number exist in the other's line 2 text? = Uncertain (Flat scenario)
        if (AddressLineContainsNumber(a2.AddressLineTwo, a1Number))
        {
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Uncertain,
                AddressComparisonResult.AddressMatchReason.FlatMissing);
        }

        // Rule 7: Does the other address's number exist in line 2 text? = Uncertain (Flat scenario)
        if (AddressLineContainsNumber(a1.AddressLineTwo, a2Number))
        {
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Uncertain,
                AddressComparisonResult.AddressMatchReason.FlatMissing);
        }

        // Rule 8: Check for range matching scenarios
        var a1IsRange = IsRange(a1Number);
        var a2IsRange = IsRange(a2Number);

        // Rule 8a: If both have the same range = Match
        if (a1IsRange && a2IsRange && a1Number.Equals(a2Number, StringComparison.OrdinalIgnoreCase))
        {
            return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Matched);
        }

        // Rule 8b: Is one a range and the other a single number within that range? = Uncertain
        if (a1IsRange && !a2IsRange && IsNumberInRange(a2Number, a1Number))
        {
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Uncertain,
                AddressComparisonResult.AddressMatchReason.NumberRange);
        }

        if (a2IsRange && !a1IsRange && IsNumberInRange(a1Number, a2Number))
        {
            return new AddressComparisonResult(
                AddressComparisonResult.AddressMatchStatus.Uncertain,
                AddressComparisonResult.AddressMatchReason.NumberRange);
        }

        // Rule 9: Everything else = Unmatched
        return new AddressComparisonResult(AddressComparisonResult.AddressMatchStatus.Unmatched);
    }

    /// <summary>
    /// Extracts the building number from an address line (e.g., "12A" from "12A High Street").
    /// Supports formats: "12", "12A", "12-14", "12A-14B"
    /// </summary>
    private static string? ExtractBuildingNumber(string? addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return null;

        var trimmed = addressLine.Trim();
        
        // Must start with a digit
        if (!char.IsDigit(trimmed[0]))
            return null;

        int i = 0;

        // Read leading digits
        while (i < trimmed.Length && char.IsDigit(trimmed[i]))
            i++;

        // Optional letter suffix (12A)
        if (i < trimmed.Length && char.IsLetter(trimmed[i]))
            i++;

        // Check for range (12-14 or 12A-14B)
        if (i < trimmed.Length && trimmed[i] == '-')
        {
            i++; // skip the dash
            
            // Read digits after dash
            while (i < trimmed.Length && char.IsDigit(trimmed[i]))
                i++;

            // Optional letter suffix after second number
            if (i < trimmed.Length && char.IsLetter(trimmed[i]))
                i++;
        }

        // Must have whitespace or end of string after the number
        if (i < trimmed.Length && !char.IsWhiteSpace(trimmed[i]))
            return null;

        return trimmed.Substring(0, i).ToUpperInvariant();
    }

    /// <summary>
    /// Checks if a building number is a range format (e.g., "12-14", "12A-14B").
    /// </summary>
    private static bool IsRange(string buildingNumber)
    {
        return buildingNumber.Contains('-');
    }

    /// <summary>
    /// Checks if a single building number exists within a range.
    /// For example: "12" is in range "12-14", "13" is in range "12-14", "14" is in range "12-14"
    /// Note: This only checks numeric values, ignoring any letter suffixes for the comparison.
    /// </summary>
    private static bool IsNumberInRange(string singleNumber, string rangeNumber)
    {
        if (!IsRange(rangeNumber))
            return false;

        var parts = rangeNumber.Split('-');
        if (parts.Length != 2)
            return false;

        // Extract numeric parts only for comparison
        var startNumStr = new string(parts[0].TakeWhile(char.IsDigit).ToArray());
        var endNumStr = new string(parts[1].TakeWhile(char.IsDigit).ToArray());
        var singleNumStr = new string(singleNumber.TakeWhile(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(startNumStr) || string.IsNullOrEmpty(endNumStr) || string.IsNullOrEmpty(singleNumStr))
            return false;

        if (!int.TryParse(startNumStr, out var start) || 
            !int.TryParse(endNumStr, out var end) || 
            !int.TryParse(singleNumStr, out var single))
        {
            return false;
        }

        return single >= start && single <= end;
    }

    /// <summary>
    /// Checks if an address line contains a specific building number anywhere in the text.
    /// This is used to detect cases where a flat/apartment number might be in one line and the building number in another.
    /// </summary>
    private static bool AddressLineContainsNumber(string? addressLine, string buildingNumber)
    {
        if (string.IsNullOrWhiteSpace(addressLine) || string.IsNullOrWhiteSpace(buildingNumber))
            return false;

        // Check if the building number appears as a standalone word in the address line
        var words = addressLine.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        return words.Any(word => word.Equals(buildingNumber, StringComparison.OrdinalIgnoreCase));
    }
}