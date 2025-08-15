namespace Shared.Models;

public class ReconciliationResponse
{
    public dynamic? Result { get; set; }

    public List<string> Errors { get; set; } = [];

    public List<Difference>? Differences { get; set; }
}