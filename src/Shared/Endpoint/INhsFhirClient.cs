using Shared.Models;

namespace Shared.Endpoint;

public interface INhsFhirClient
{
    Task<SearchResult?> PerformSearch(SearchQuery query);
    Task<DemographicResult> PerformSearchByNhsId(string nhsId);
}