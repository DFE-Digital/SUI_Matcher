using System.Security.Cryptography;
using System.Text;

using Shared.Models;

namespace Shared.Util;

public static class HashUtil
{
    private static string FormatGender(string inputGender)
    {
        string gender = "";
        if (string.IsNullOrWhiteSpace(inputGender))
        {
            gender = "";
        }
        else if (int.TryParse(inputGender, out _))
        {
            gender = PersonSpecificationUtils.ToGenderFromNumber(inputGender);
        }
        else
        {
            gender = inputGender!;
        }
        return gender;
    }
    private static string FormatPostalCode(string inputPostalCode)
    {
        if (string.IsNullOrWhiteSpace(inputPostalCode))
        {
            return "";
        }
        return new string(inputPostalCode
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray())
            .ToLowerInvariant();
    }

    private static string FormatName(string name) => string.IsNullOrWhiteSpace(name) ? "" : name!.ToLowerInvariant();
    private static string FormatBirthDate(DateOnly? birthDate) => birthDate is DateOnly date ? date.ToString("dd/MM/yyyy") : "";

    private static string CreateHash(string data)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        return builder.ToString();
    }
    private static string PrepareDataString(string given, string family, string birthDate, string gender, string postalCode)
    {
        return $"{given}{family}{birthDate}{gender}{postalCode}";
    }

    public static string StoreUniqueSearchIdFor(PersonSpecification personSpecification)
    {
        var given = FormatName(personSpecification.Given!);
        var family = FormatName(personSpecification.Family!);
        var gender = FormatGender(personSpecification.Gender!);
        var birthDate = FormatBirthDate(personSpecification.BirthDate!);
        var postalCode = FormatPostalCode(personSpecification.AddressPostalCode!);

        var data = PrepareDataString(given, family, birthDate, gender, postalCode);
        var hash = CreateHash(data);

        Activity.Current?.SetBaggage("SearchId", hash);
        return hash;
    }

    public static string StoreUniqueSearchIdFor(MatchPersonResult personSpecification)
    {
        var given = FormatName(personSpecification.Given!);
        var family = FormatName(personSpecification.Family!);
        var gender = FormatGender(personSpecification.Gender!);
        var birthDate = FormatBirthDate(personSpecification.BirthDate!);
        var postalCode = FormatPostalCode(personSpecification.AddressPostalCode!);

        var data = PrepareDataString(given, family, birthDate, gender, postalCode);
        var hash = CreateHash(data);

        Activity.Current?.SetBaggage("SearchId", hash);
        return hash;
    }

}