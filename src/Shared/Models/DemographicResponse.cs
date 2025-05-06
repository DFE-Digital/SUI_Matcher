namespace Shared.Models;

public class DemographicResponse
{
    public dynamic? Result { get; set; }
    public List<string> Errors { get; set; } = [];
}