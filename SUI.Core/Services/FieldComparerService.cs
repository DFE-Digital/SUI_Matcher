using Hl7.Fhir.Model;
using Shared.Models;

namespace SUI.Core.Services;

public static class FieldComparerService
{
    /// <summary>
    /// Compares the fields of the SearchQuery and the Patient object.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="patient">Fhir Patient</param>
    /// <returns>The names of the fields that have different values between query and patient</returns>
    public static List<string> ComparePatientFields(SearchQuery query, Patient patient)
    {
        var differentFields = new List<string>();

        CompareField(query.Birthdate?.FirstOrDefault(), $"eq{patient.BirthDate}", nameof(SearchQuery.Birthdate), differentFields);
        CompareField(query.AddressPostalcode, patient.Address.FirstOrDefault()?.PostalCode, nameof(SearchQuery.AddressPostalcode), differentFields);
        CompareField(query.Gender, patient.Gender?.ToString(), nameof(SearchQuery.Gender), differentFields);
        CompareField(query.Family, patient.Name.FirstOrDefault()?.Family, nameof(SearchQuery.Family), differentFields);
        CompareField(query.Given?.FirstOrDefault(), patient.Name.FirstOrDefault()?.Given.FirstOrDefault(), nameof(SearchQuery.Given), differentFields);

        var email = patient.Telecom.FirstOrDefault(x => x.System == ContactPoint.ContactPointSystem.Email)?.Value;
        CompareField(query.Email, email, nameof(SearchQuery.Email), differentFields);
        
        var phone = patient.Telecom.FirstOrDefault(x => x.System == ContactPoint.ContactPointSystem.Phone)?.Value;
        CompareField(query.Phone, phone, nameof(SearchQuery.Phone), differentFields);

        return differentFields;
    }

    private static void CompareField(string? queryValue, string? resourceValue, string fieldName, List<string> differentFields)
    {
        // Assumption that we don't compare if our input is null
        if (queryValue is null) return;

        if (!queryValue.Equals(resourceValue, StringComparison.OrdinalIgnoreCase))
        {
            differentFields.Add(fieldName);
        }
    }
}