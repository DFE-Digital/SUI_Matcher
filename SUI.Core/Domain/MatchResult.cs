using Shared.Models;

namespace SUI.Core.Domain;

public class MatchResult
{
    public ValidationResponse? Errors { get; set; }
    public SearchResult? Result { get; set; }
    public MatchStatus Status { get; set; }
    
    public decimal? Score { get; set; }
    
    public int? ProcessStage { get; set; }

    public MatchResult(ValidationResponse errors)
    {
        Status = MatchStatus.Error;
        Errors = errors;
    }

    public MatchResult(SearchResult result, MatchStatus status, int processStage)
    {
        Result = result;
        Status = status;
        ProcessStage = processStage;
    }

    public MatchResult(MatchStatus status)
    {
        Status = status;
    }

    public MatchResult() { }
}