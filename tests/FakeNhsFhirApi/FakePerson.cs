using CsvHelper.Configuration.Attributes;

namespace FakeNhsFhirApi;

public class FakePerson
{
    [Name("DOB")] public string Dob { get; set; } = null!;

    [Name("Email")]
    public string Email { get; set; } = null!;

    [Name("Surname")]
    public string Family { get; set; } = null!;

    [Name("Gender")]
    public string Gender { get; set; } = null!;

    [Name("GivenName")]
    public string Given { get; set; } = null!;
    [Ignore]
    public string NhsId { get; set; } = null!;

    [Name("Phone")]
    public string Phone { get; set; } = null!;
}