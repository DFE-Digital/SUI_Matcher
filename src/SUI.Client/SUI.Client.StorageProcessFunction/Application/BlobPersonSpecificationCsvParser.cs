using Shared.Models;
using Shared.Util;
using SUI.Client.Core.Infrastructure.FileSystem;

namespace SUI.StorageProcessFunction.Application;

public sealed class BlobPersonSpecificationCsvParser : IBlobPersonSpecificationCsvParser
{
    private static readonly string[] AcceptedDateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];
    private static readonly string[] RequiredHeaders =
    [
        "GivenName",
        "FamilyName",
        "DOB",
        "Postcode",
    ];

    public async Task<IReadOnlyList<PersonSpecification>> ParseAsync(
        BlobFileContent blobFile,
        CancellationToken cancellationToken
    )
    {
        await using var stream = blobFile.Content.ToStream();
        using var reader = new StreamReader(stream);
        (HashSet<string> headers, List<Dictionary<string, string>> records) =
            await CsvRecordReader.ReadCsvTextAsync(reader, cancellationToken);

        if (headers.Count == 0 || records.Count == 0)
        {
            throw new InvalidOperationException(
                $"Blob '{blobFile.Blob.BlobName}' did not contain any CSV records."
            );
        }

        var missingHeaders = RequiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"Blob '{blobFile.Blob.BlobName}' is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }

        var people = new List<PersonSpecification>(records.Count);
        foreach (var record in records)
        {
            var dob = record.GetFirstValueOrDefault(["DOB"]).Trim();
            var birthDate = dob.ToDateOnly(AcceptedDateFormats);

            if (birthDate is null)
            {
                throw new InvalidOperationException(
                    $"Blob '{blobFile.Blob.BlobName}' contains an invalid DOB value '{dob}'."
                );
            }

            people.Add(
                new PersonSpecification
                {
                    Given = record.GetFirstValueOrDefault(["GivenName"]).Trim(),
                    Family = record.GetFirstValueOrDefault(["FamilyName"]).Trim(),
                    BirthDate = birthDate.Value,
                    RawBirthDate = [dob],
                    AddressPostalCode = record.GetFirstValueOrDefault(["Postcode"]).Trim(),
                }
            );
        }

        return people;
    }
}
