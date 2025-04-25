using Hl7.Fhir.Model;
using Shared.Models;
using SUI.Core.Services;

namespace SUI.Test.Unit.Core.Services;

[TestClass]
public class FieldComparerServiceTests
{
    [TestMethod]
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
        CollectionAssert.Contains(differences, nameof(SearchQuery.Birthdate));
        CollectionAssert.Contains(differences, nameof(SearchQuery.AddressPostalcode));
        CollectionAssert.Contains(differences, nameof(SearchQuery.Phone));
        CollectionAssert.Contains(differences, nameof(SearchQuery.Gender));
        CollectionAssert.Contains(differences, nameof(SearchQuery.Given));
        CollectionAssert.DoesNotContain(differences, nameof(SearchQuery.Family));
        CollectionAssert.DoesNotContain(differences, nameof(SearchQuery.Email));
    }
    
    [TestMethod]
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
        Assert.IsFalse(differences.Contains(nameof(SearchQuery.Birthdate)));
    }
}