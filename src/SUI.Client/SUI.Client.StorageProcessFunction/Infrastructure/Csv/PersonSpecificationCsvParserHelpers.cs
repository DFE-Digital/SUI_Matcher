using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Shared.Models;
using Shared.Util;

namespace SUI.StorageProcessFunction.Infrastructure.Csv;

internal static class PersonSpecificationCsvParserHelpers
{
    private static readonly string[] AcceptedDateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];

    public static CsvReader CreateCsvReader(BinaryData content)
    {
        var reader = new StreamReader(content.ToStream(), leaveOpen: true);
        return new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            }
        );
    }

    public static void ValidateHeaders(CsvReader csv, string fileName, string[] requiredHeaders)
    {
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

        var missingHeaders = requiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"File '{fileName}' is missing required headers: {string.Join(", ", missingHeaders)}."
            );
        }
    }

    internal static List<PersonSpecification> ReadPeople(
        CsvReader csv,
        string fileName,
        string dobFieldName,
        Func<CsvReader, DateOnly, PersonSpecification> createPerson,
        CancellationToken cancellationToken
    )
    {
        var persons = new List<PersonSpecification>();
        var hasRows = false;
        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            hasRows = true;

            var dob = (csv.GetField(dobFieldName) ?? string.Empty).Trim();
            var birthDate = dob.ToDateOnly(AcceptedDateFormats);

            if (birthDate is null)
            {
                throw new InvalidOperationException(
                    $"File '{fileName}' contains an invalid DOB value '{dob}'."
                );
            }

            persons.Add(createPerson(csv, birthDate.Value));
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
