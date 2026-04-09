using Shared.Models;
using Shared.Util;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Infrastructure.FileSystem;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public class CsvPersonSpecParser(string parserToUse) : IPersonSpecParser<Dictionary<string, string>>
{
    public PersonSpecification Parse(Dictionary<string, string> record)
    {
        return parserToUse switch
        {
            "TypeOne" => ParseTypeOne(record),
            _ => throw new InvalidOperationException($"Unknown parser type: {parserToUse}."),
        };
    }

    private static PersonSpecification ParseTypeOne(Dictionary<string, string> record)
    {
        string[] acceptedDateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];

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
