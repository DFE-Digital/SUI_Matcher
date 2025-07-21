namespace SUI.Client.Core.Models;

public class MatchPersonPayload
{
    public string? Given { get; set; }
    public string? Family { get; set; }
    public string? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressPostalCode { get; set; }
}

public class MatchPersonPayloadFromDbsCsv
{
    public string? Forename { get; set; }
    public string? Surname { get; set; }
    public string? Dob { get; set; }
    public string? Gender { get; set; }
    public string? PostCode { get; set; }
}