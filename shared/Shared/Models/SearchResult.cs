namespace Shared.Models;

public class SearchResult
{
    public ResultType Type { get; set; } = ResultType.Unmatched;
    
    public string? NhsNumber { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public enum ResultType
    {
        Matched,
        Unmatched,
        MultiMatched,
        Error
    }
}