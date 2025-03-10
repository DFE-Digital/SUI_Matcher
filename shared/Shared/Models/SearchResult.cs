namespace Shared.Models;

public class SearchResult
{
    public ResultType Type { get; set; } = ResultType.Unmatched;

    public decimal? Score { get; set; }

    public string? NhsNumber { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public enum ResultType
    {
        Matched,
        Unmatched,
        MultiMatched,
        Error
    }

    public static SearchResult Match(string nhsNumber, decimal? score) => new()
    {
        Type = ResultType.Matched,
        NhsNumber = nhsNumber,
        Score = score,
    };

    public static SearchResult Unmatched() => new() { Type = ResultType.Unmatched };

    public static SearchResult MultiMatched() => new() { Type = ResultType.MultiMatched };

    public static SearchResult Error(string errorMessage) => new() { Type = ResultType.Error, ErrorMessage = errorMessage };
}