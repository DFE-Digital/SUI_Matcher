namespace Shared.Models;

public class MatchPersonResult
{
    public string? Given { get; set; }
    public string? Family { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? AddressPostalCode { get; set; }
    public string? NhsNumber { get; set; }
}