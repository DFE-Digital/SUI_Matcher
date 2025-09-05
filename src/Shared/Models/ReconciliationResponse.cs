namespace Shared.Models;

public class ReconciliationResponse
{
    public NhsPerson? Person { get; set; }

    public List<string> Errors { get; set; } = [];

    public List<Difference>? Differences { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReconciliationStatus Status { get; set; }
}