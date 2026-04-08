using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Shared.Models;
using Shared.Util;
using SUI.StorageProcessFunction.Application.Interfaces;

namespace SUI.StorageProcessFunction.Infrastructure.Csv;

public sealed class PersonSpecificationCsvParser : IPersonSpecificationCsvParser
{
    private static readonly string[] AcceptedDateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];
    private static readonly string[] RequiredHeaders =
    [
        "GivenName",
        "FamilyName",
        "DOB",
        "Postcode",
    ];

    public async IAsyncEnumerable<PersonSpecification> ParseAsync(
        Stream content,
        string fileName,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            }
        );

        if (!await csv.ReadAsync())
        {
            throw new InvalidOperationException(
                $"File '{fileName}' did not contain any CSV records."
            );
        }

        csv.ReadHeader();
        var headers = (csv.HeaderRecord ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingHeaders = RequiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"File '{fileName}' is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }

        var hasRows = false;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            hasRows = true;

            var dob = (csv.GetField("DOB") ?? string.Empty).Trim();
            var birthDate = dob.ToDateOnly(AcceptedDateFormats);

            if (birthDate is null)
            {
                throw new InvalidOperationException(
                    $"File '{fileName}' contains an invalid DOB value '{dob}'."
                );
            }

            yield return new PersonSpecification
            {
                Given = (csv.GetField("GivenName") ?? string.Empty).Trim(),
                Family = (csv.GetField("FamilyName") ?? string.Empty).Trim(),
                BirthDate = birthDate.Value,
                Email = (csv.GetField("Email") ?? string.Empty).Trim(),
                Phone = (csv.GetField("Phone") ?? string.Empty).Trim(),
                RawBirthDate = [dob],
                Gender = (csv.GetField("Gender") ?? string.Empty).Trim(),
                AddressPostalCode = (csv.GetField("Postcode") ?? string.Empty).Trim(),
            };
        }

        if (!hasRows)
        {
            throw new InvalidOperationException(
                $"File '{fileName}' did not contain any CSV records."
            );
        }
    }

    public List<PersonSpecification> ParseListAsync(
        BinaryData content,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var persons = new List<PersonSpecification>();
        using var reader = new StreamReader(content.ToStream(), leaveOpen: true);
        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            }
        );

        if (!csv.Read())
        {
            throw new InvalidOperationException(
                $"File '{fileName}' did not contain any CSV records."
            );
        }

        csv.ReadHeader();
        var headers = (csv.HeaderRecord ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingHeaders = RequiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"File '{fileName}' is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }

        var hasRows = false;
        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            hasRows = true;

            var dob = (csv.GetField("DOB") ?? string.Empty).Trim();
            var birthDate = dob.ToDateOnly(AcceptedDateFormats);

            if (birthDate is null)
            {
                throw new InvalidOperationException(
                    $"File '{fileName}' contains an invalid DOB value '{dob}'."
                );
            }

            persons.Add(
                new PersonSpecification
                {
                    Given = (csv.GetField("GivenName") ?? string.Empty).Trim(),
                    Family = (csv.GetField("FamilyName") ?? string.Empty).Trim(),
                    BirthDate = birthDate.Value,
                    Email = (csv.GetField("Email") ?? string.Empty).Trim(),
                    Phone = (csv.GetField("Phone") ?? string.Empty).Trim(),
                    RawBirthDate = [dob],
                    Gender = (csv.GetField("Gender") ?? string.Empty).Trim(),
                    AddressPostalCode = (csv.GetField("Postcode") ?? string.Empty).Trim(),
                }
            );
        }

        if (!hasRows)
        {
            throw new InvalidOperationException(
                $"File '{fileName}' did not contain any CSV records."
            );
        }

        return persons;
    }
}
