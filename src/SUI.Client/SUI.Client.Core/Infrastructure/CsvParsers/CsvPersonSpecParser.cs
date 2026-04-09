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

    /// <summary>
    ///  Extracts a list of person specifications from a dictionary list.
    /// </summary>
    /// <param name="records"></param>
    /// <param name="requiredHeaders"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Throws if required headers are not present</exception>
    public List<PersonSpecification> Parse(
        List<Dictionary<string, string>> records,
        HashSet<string> requiredHeaders
    )
    {
        return _parserToUse switch
        {
            "TypeOne" => ParseTypeOne(records, requiredHeaders),
            _ => throw new InvalidOperationException($"Unknown parser type: {_parserToUse}."),
        };
    }

    private static List<PersonSpecification> ParseTypeOne(
        List<Dictionary<string, string>> records,
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

        List<PersonSpecification> people = [];
        foreach (var record in records)
        {
            var dob = (record.GetFirstValueOrDefault(["DOB"])).Trim();
            var birthDate = dob.ToDateOnly(acceptedDateFormats);
            people.Add(
                new PersonSpecification
                {
                    Given = record.GetFirstValueOrDefault(["GivenName"]),
                    Family = record.GetFirstValueOrDefault(["FamilyName"]),
                    BirthDate = birthDate,
                    Email = record.GetFirstValueOrDefault(["Email"]),
                    AddressPostalCode = record.GetFirstValueOrDefault(["Postcode"]),
                    Gender = record.GetFirstValueOrDefault(["Gender"]),
                    Phone = record.GetFirstValueOrDefault(["Phone"]),
                    RawBirthDate = [dob],
                }
            );
        }

        return people;
    }
}
