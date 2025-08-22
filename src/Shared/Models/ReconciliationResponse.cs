namespace Shared.Models;

public class ReconciliationResponse
{
    public NhsPerson? Person { get; set; }

    public List<string> Errors { get; set; } = [];

    public List<Difference>? Differences { get; set; }

    public ReconciliationStatus Status { get; set; }
}