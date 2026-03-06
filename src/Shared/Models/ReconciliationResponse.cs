namespace Shared.Models;

public class ReconciliationResponse
{
    public NhsPerson? Person { get; set; }

    public List<string> Errors { get; set; } = [];

    public List<Difference> Differences { get; set; } = [];
    public List<string> DifferenceFields { get; set; } = [];
    public List<string> MissingLocalFields { get; set; } = [];
    public List<string> MissingNhsFields { get; set; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReconciliationStatus Status { get; set; }

    public MatchResult? MatchingResult { get; init; }
}