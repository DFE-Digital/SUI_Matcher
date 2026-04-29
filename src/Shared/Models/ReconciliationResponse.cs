namespace Shared.Models;

public class ReconciliationResponse
{
    public NhsPerson? Person { get; set; }

    public List<string> Errors { get; set; } = [];
    public List<string> DifferenceFields { get; init; } = [];
    public List<string> MissingLocalFields { get; init; } = [];
    public List<string> MissingNhsFields { get; init; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReconciliationStatus Status { get; set; }

    public MatchResult? MatchingResult { get; init; }
    public string? SearchId { get; set; }
}
