using Shared.Models;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Infrastructure.Csv;

public sealed class PersonSpecificationCsvParser : IPersonSpecificationCsvParser
{
    private static readonly string[] RequiredHeaders =
    [
        "GivenName",
        "FamilyName",
        "DOB",
        "Postcode",
    ];

    public List<PersonSpecification> ParseListAsync(
        BinaryData content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        using var csv = PersonSpecificationCsvParserHelpers.CreateCsvReader(content);
        PersonSpecificationCsvParserHelpers.ValidateHeaders(csv, fileName, RequiredHeaders);

        return PersonSpecificationCsvParserHelpers.ReadPeople(
            csv,
            fileName,
            dobFieldName: "DOB",
            createPerson: (row, dob) =>
                new PersonSpecification
                {
                    Given = (row.GetField("GivenName") ?? string.Empty).Trim(),
                    Family = (row.GetField("FamilyName") ?? string.Empty).Trim(),
                    BirthDate = dob,
                    Email = (row.GetField("Email") ?? string.Empty).Trim(),
                    Phone = (row.GetField("Phone") ?? string.Empty).Trim(),
                    RawBirthDate = [(row.GetField("DOB") ?? string.Empty).Trim()],
                    Gender = (row.GetField("Gender") ?? string.Empty).Trim(),
                    AddressPostalCode = (row.GetField("Postcode") ?? string.Empty).Trim(),
                },
            cancellationToken
        );
    }
}
