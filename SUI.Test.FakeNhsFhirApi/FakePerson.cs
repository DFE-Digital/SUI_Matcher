using CsvHelper.Configuration.Attributes;

namespace SUI.Test.FakeNhsFhirApi;

public class FakePerson
{
    [Name("DOB")]
    public string Dob { get; set; }

    [Name("Email")]
    public string Email { get; set; }

    [Name("Surname")]
    public string Family { get; set; }

    [Name("Gender")]
    public string Gender { get; set; }

    [Name("GivenName")]
    public string Given { get; set; }
    [Ignore]
    public string NhsId { get; set; }

    [Name("Phone")]
    public string Phone { get; set; }
}