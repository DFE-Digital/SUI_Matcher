namespace Shared.Models;

public class MatchResult
{
    [JsonPropertyName("matchStatus")]
    public MatchStatus MatchStatus { get; set; }

    [JsonPropertyName("nhsNumber")]
    public string? NhsNumber { get; set; }

    [JsonPropertyName("processStage")]
    public int? ProcessStage { get; set; }

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }
}