using System.Security.Cryptography;
using System.Text;

using Shared.Models;

namespace Shared.Util;

public static class HashUtil
{
    public static string StoreUniqueSearchIdFor(PersonSpecification personSpecification)
    {
        var given = string.IsNullOrWhiteSpace(personSpecification.Given) ? "" : personSpecification.Given!.ToLowerInvariant();
        var family = string.IsNullOrWhiteSpace(personSpecification.Family) ? "" : personSpecification.Family!.ToLowerInvariant();

        string gender = "";
        if (string.IsNullOrWhiteSpace(personSpecification.Gender))
        {
            gender = "";
        }
        else if (int.TryParse(personSpecification.Gender, out _))
        {
            gender = PersonSpecificationUtils.ToGenderFromNumber(personSpecification.Gender);
        }
        else
        {
            gender = personSpecification.Gender!;
        }

        string birthDate = "";
        if (personSpecification.BirthDate is DateOnly date)
        {
            birthDate = date.ToString("dd/MM/yyyy");
        }

        string postalCode = "";
        if (!string.IsNullOrWhiteSpace(personSpecification.AddressPostalCode))
        {
            postalCode = new string(personSpecification.AddressPostalCode
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .ToLowerInvariant();
        }

        var data = $"{given}{family}{birthDate}{gender}{postalCode}";

        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetBaggage("SearchId", hash);

        return hash;
    }

    public static string StoreUniqueSearchIdFor(MatchPersonResult personSpecification)
    {
        var given = string.IsNullOrWhiteSpace(personSpecification.Given) ? "" : personSpecification.Given!.ToLowerInvariant();
        var family = string.IsNullOrWhiteSpace(personSpecification.Family) ? "" : personSpecification.Family!.ToLowerInvariant();

        string gender = "";
        if (string.IsNullOrWhiteSpace(personSpecification.Gender))
        {
            gender = "";
        }
        else if (int.TryParse(personSpecification.Gender, out _))
        {
            gender = PersonSpecificationUtils.ToGenderFromNumber(personSpecification.Gender);
        }
        else
        {
            gender = personSpecification.Gender!;
        }

        string birthDate = "";
        if (personSpecification.BirthDate is DateOnly date)
        {
            birthDate = date.ToString("dd/MM/yyyy");
        }

        string postalCode = "";
        if (!string.IsNullOrWhiteSpace(personSpecification.AddressPostalCode))
        {
            postalCode = new string(personSpecification.AddressPostalCode
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .ToLowerInvariant();
        }

        var data = $"{given}{family}{birthDate}{gender}{postalCode}";

        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetBaggage("SearchId", hash);

        return hash;
    }
}