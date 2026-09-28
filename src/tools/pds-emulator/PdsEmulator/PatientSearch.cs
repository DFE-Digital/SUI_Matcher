using System.Globalization;

using Microsoft.AspNetCore.Http;

namespace PdsEmulator;

public static class PatientSearch
{
    private static readonly string[] DatePrefixes = ["eq", "ge", "le", "gt", "lt"];

    public static IEnumerable<Patient> Apply(
        IEnumerable<Patient> patients,
        IQueryCollection requestQuery
    )
    {
        var query = patients;

        query = ApplyStringConstraint(
            query,
            Values(requestQuery, "given"),
            (patient, value) =>
                patient.Name?.Any(name =>
                    name.Given?.Any(given => EqualsIgnoreCase(given, value)) == true
                ) == true
        );
        query = ApplyStringConstraint(
            query,
            Values(requestQuery, "family"),
            (patient, value) =>
                patient.Name?.Any(name => EqualsIgnoreCase(name.Family, value)) == true
        );
        query = ApplyBirthDateConstraints(query, Values(requestQuery, "birthdate"));
        query = ApplyStringConstraint(
            query,
            Values(requestQuery, "address-postalcode"),
            MatchesPostcode
        );
        query = ApplyStringConstraint(
            query,
            Values(requestQuery, "gender"),
            (patient, value) => EqualsIgnoreCase(patient.Gender, value)
        );
        query = ApplyStringConstraint(
            query,
            Values(requestQuery, "email"),
            (patient, value) => MatchesContactPoint(patient, "email", value)
        );
        query = ApplyStringConstraint(
            query,
            Values(requestQuery, "phone"),
            (patient, value) => MatchesContactPoint(patient, "phone", value)
        );

        return query;
    }

    private static IEnumerable<Patient> ApplyStringConstraint(
        IEnumerable<Patient> patients,
        string[] values,
        Func<Patient, string, bool> matches
    ) => values.Length == 0 ? patients : patients.Where(patient => values.All(value => matches(patient, value)));

    private static IEnumerable<Patient> ApplyBirthDateConstraints(
        IEnumerable<Patient> patients,
        string[] constraints
    )
    {
        if (constraints.Length == 0)
        {
            return patients;
        }

        return patients.Where(patient =>
            DateOnly.TryParseExact(
                patient.BirthDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var patientBirthDate
            ) && constraints.All(constraint => MatchesBirthDate(patientBirthDate, constraint))
        );
    }

    private static bool MatchesBirthDate(DateOnly patientBirthDate, string constraint)
    {
        var prefix = DatePrefixes.FirstOrDefault(value => constraint.StartsWith(value, StringComparison.Ordinal));
        var operation = prefix ?? "eq";
        var dateText = prefix == null ? constraint : constraint[prefix.Length..];

        if (
            !DateOnly.TryParseExact(
                dateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var expectedBirthDate
            )
        )
        {
            return false;
        }

        return operation switch
        {
            "ge" => patientBirthDate >= expectedBirthDate,
            "le" => patientBirthDate <= expectedBirthDate,
            "gt" => patientBirthDate > expectedBirthDate,
            "lt" => patientBirthDate < expectedBirthDate,
            _ => patientBirthDate == expectedBirthDate,
        };
    }

    private static bool MatchesPostcode(Patient patient, string expected)
    {
        var normalizedExpected = NormalizePostcode(expected);
        var isWildcard = normalizedExpected.EndsWith('*');
        var expectedValue = isWildcard ? normalizedExpected[..^1] : normalizedExpected;

        return patient.Address?.Any(address =>
        {
            var actual = NormalizePostcode(address.PostalCode);
            return isWildcard
                ? actual.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase)
                : EqualsIgnoreCase(actual, expectedValue);
        }) == true;
    }

    private static bool MatchesContactPoint(Patient patient, string system, string expected) =>
        patient.Telecom?.Any(contact =>
            EqualsIgnoreCase(contact.System, system) && EqualsIgnoreCase(contact.Value, expected)
        ) == true;

    private static string NormalizePostcode(string? value) =>
        string.Concat((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));

    private static string[] Values(IQueryCollection query, string name) =>
        query.TryGetValue(name, out var values)
            ? values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).ToArray()
            : [];

    private static bool EqualsIgnoreCase(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
}