using Shared.Models;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public class ProcessedMatchRecord<TSource>
{
    // The untouched original record (Dictionary or GraphType)
    public TSource OriginalData { get; set; } = default!;

    // The result from the API
    public PersonMatchResponse? ApiResult { get; set; }

    // Metadata for the edges to know how to handle this row
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
