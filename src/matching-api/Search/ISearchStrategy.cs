using Shared.Models;

namespace MatchingApi.Search;

public interface ISearchStrategy
{
    OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model, int? version = null);
    int GetAlgorithmVersion();

    int[] GetAllAlgorithmVersions();
}