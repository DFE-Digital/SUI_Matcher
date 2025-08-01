namespace Shared.Models;

public class MatchResult2
{
    public ValidationResponse? Errors { get; set; }
    public SearchResult? Result { get; set; }
    public MatchStatus Status { get; set; }

    public decimal? Score { get; set; }

    public int? ProcessStage { get; set; }

    public MatchResult2(ValidationResponse errors)
    {
        Status = MatchStatus.Error;
        Errors = errors;
    }

    public MatchResult2(SearchResult result, MatchStatus status, decimal score, int processStage)
    {
        Result = result;
        Status = status;
        Score = score;
        ProcessStage = processStage;
    }

    public MatchResult2(SearchResult result, MatchStatus status, int processStage)
    {
        Result = result;
        Status = status;
        ProcessStage = processStage;
    }

    public MatchResult2(MatchStatus status, int processStage)
    {
        Status = status;
        ProcessStage = processStage;
    }

    public MatchResult2() { }
}