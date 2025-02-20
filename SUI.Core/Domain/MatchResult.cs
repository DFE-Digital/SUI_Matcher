using Shared.Models;

namespace SUI.Core.Domain;

public class MatchResult
{
    public ValidationResponse? Errors { get; set; }
    public SearchResult? Result { get; set; }
    public MatchStatus Status { get; set; }

    public MatchResult(ValidationResponse errors)
    {
        Status = MatchStatus.Error;
        Errors = errors;
    }

    public MatchResult(SearchResult result, MatchStatus status)
    {
        Result = result;
        Status = status;
    }

    public MatchResult(MatchStatus status)
    {
        Status = status;
    }

    public MatchResult() { }
}