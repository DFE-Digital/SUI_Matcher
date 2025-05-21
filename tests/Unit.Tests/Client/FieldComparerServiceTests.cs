using ExternalApi.Services;

using Hl7.Fhir.Model;

using Shared.Models;

namespace Unit.Tests.Client;

public class FieldComparerServiceTests
{
    [Fact]
    public void ComparePatientFields_ShouldReturnDifferences_WhenFieldsDoNotMatch()
    {
        // Arrange
        var query = new SearchQuery
        {
            Birthdate = ["eq1990-01-01"],
            AddressPostalcode = "12345",
            Phone = "555-1234",
            Gender = "male",
            Family = "Smith",
            Given = ["John"],
            Email = "john.doe@example.com"
        };

        var patient = new Patient
        {
            BirthDate = "1980-01-01",
            Address = [new Address { PostalCode = "54321" }],
            Telecom =
            [
                new ContactPoint { Value = "555-5678", System = ContactPoint.ContactPointSystem.Phone },
                new ContactPoint { Value = "john.doe@example.com", System = ContactPoint.ContactPointSystem.Email }
            ],
            Gender = AdministrativeGender.Female,
            Name = [new HumanName { Family = "Smith", Given = ["Jane"] }]
        };

        // Act
        var differences = FieldComparerService.ComparePatientFields(query, patient);

        // Assert
        Assert.Contains(nameof(SearchQuery.Birthdate), differences);
        Assert.Contains(nameof(SearchQuery.AddressPostalcode), differences);
        Assert.Contains(nameof(SearchQuery.Phone), differences);
        Assert.Contains(nameof(SearchQuery.Gender), differences);
        Assert.Contains(nameof(SearchQuery.Given), differences);
        Assert.DoesNotContain(nameof(SearchQuery.Family), differences);
        Assert.DoesNotContain(nameof(SearchQuery.Email), differences);
    }

    [Fact]
    public void ShouldIdentifyBirthdateAsTheSame()
    {
        // Arrange
        var query = new SearchQuery
        {
            Birthdate = ["eq1980-01-01"]
        };

        var patient = new Patient
        {
            BirthDate = "1980-01-01"
        };

        // Act
        var differences = FieldComparerService.ComparePatientFields(query, patient);

        // Assert
        Assert.DoesNotContain(nameof(SearchQuery.Birthdate), differences);
    }
}