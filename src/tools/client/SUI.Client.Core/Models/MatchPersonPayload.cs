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
    public Dictionary<string, object> OptionalProperties { get; set; } = new();

    // Form of configuration. See API docs
    public string? SearchStrategy { get; set; }
    public int? StrategyVersion { get; set; }
}