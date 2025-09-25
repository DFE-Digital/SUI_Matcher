using Shared.Models;

namespace MatchingApi.Search;

public interface ISearchStrategy
{
    OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model);
    int GetAlgorithmVersion();
}