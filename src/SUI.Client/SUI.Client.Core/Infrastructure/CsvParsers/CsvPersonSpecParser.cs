using Shared.Models;
using Shared.Util;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Infrastructure.FileSystem;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public class CsvPersonSpecParser : IPersonSpecParser<Dictionary<string, string>>
{
    private readonly string _parserToUse;

    public CsvPersonSpecParser(string parserToUse)
    {
        _parserToUse = parserToUse;
    }

    public PersonSpecification Parse(
        Dictionary<string, string> record,
        HashSet<string> requiredHeaders
    )
    {
        return _parserToUse switch
        {
            "TypeOne" => ParseTypeOne(record, requiredHeaders),
            _ => throw new InvalidOperationException($"Unknown parser type: {_parserToUse}."),
        };
    }

    private static PersonSpecification ParseTypeOne(
        Dictionary<string, string> record,
        HashSet<string> headers
    )
    {
        string[] acceptedDateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];
        string[] requiredHeaders = ["GivenName", "FamilyName", "DOB", "Postcode"];

        var missingHeaders = requiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"CSV is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }

        var dob = (record.GetFirstValueOrDefault(["DOB"])).Trim();
        var birthDate = dob.ToDateOnly(acceptedDateFormats);
        return new PersonSpecification
        {
            Given = record.GetFirstValueOrDefault(["GivenName"]),
            Family = record.GetFirstValueOrDefault(["FamilyName"]),
            BirthDate = birthDate,
            Email = record.GetFirstValueOrDefault(["Email"]),
            AddressPostalCode = record.GetFirstValueOrDefault(["Postcode"]),
            Gender = record.GetFirstValueOrDefault(["Gender"]),
            Phone = record.GetFirstValueOrDefault(["Phone"]),
            RawBirthDate = [dob],
        };
    }
}
