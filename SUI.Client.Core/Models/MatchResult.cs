namespace SUI.Client.Core.Models;

public class MatchResult
{
    public MatchStatus MatchStatus { get; set; }
    public string? NhsNumber { get; set; }
    public int? ProcessStage { get; set; }
    public decimal? Score { get; set; }
}
