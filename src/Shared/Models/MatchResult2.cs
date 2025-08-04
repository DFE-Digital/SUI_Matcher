namespace Shared.Models;

public class MatchResult2
{
    public ValidationResponse? Errors { get; set; }
    public SearchResult? Result { get; set; }
    public MatchStatus Status { get; set; }

    public decimal? Score { get; set; }

    public string? ProcessStage { get; set; }

    public MatchResult2(ValidationResponse errors)
    {
        Status = MatchStatus.Error;
        Errors = errors;
    }

    public MatchResult2(MatchStatus status) => Status = status;

    public MatchResult2(SearchResult result, MatchStatus status, string processStage)
    {
        Result = result;
        Status = status;
        ProcessStage = processStage;
    }

    public MatchResult2(SearchResult result, MatchStatus status, decimal score, string processStage)
    {
        Result = result;
        Status = status;
        Score = score;
        ProcessStage = processStage;
    }

    public MatchResult2(MatchStatus status, string processStage)
    {
        Status = status;
        ProcessStage = processStage;
    }

    public MatchResult2() { }
}