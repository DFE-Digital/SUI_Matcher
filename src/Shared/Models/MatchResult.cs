using Shared.Constants;

namespace Shared.Models;

public class MatchResult
{
    [JsonPropertyName("matchStatus")]
    public MatchStatus MatchStatus { get; set; }

    [JsonPropertyName("nhsNumber")]
    public string? NhsNumber { get; set; }

    [JsonPropertyName("processStage")]
    public string? ProcessStage { get; set; }

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonIgnore]
    public bool IsHighConfidenceMatch =>
        MatchStatus == MatchStatus.Match && Score is > MatchScoreConstants.MatchSuccessThreshold;
}
