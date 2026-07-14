using System.Diagnostics.CodeAnalysis;

namespace FakeEclipseGraphQLApi.Models;

[ExcludeFromCodeCoverage]
public class Person : IPersonByCriteria_PersonByCriteria_Results
{
    public string Id { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Forename { get; set; }
    public string? Surname { get; set; }
    public string? Gender { get; set; }
    public string? NhsNumber { get; set; }
    public int ObjectVersion { get; set; } = 1;
    public DateRange? DateOfBirth { get; set; }
    public List<Address> Addresses { get; set; } = new();
    public Address? PreferredAddress { get; set; }
}