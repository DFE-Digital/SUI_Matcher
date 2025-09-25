using Shared;
using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Quite a broad strategy that uses a variety of exact and fuzzy searches with different combinations of parameters.
/// </summary>
public class SearchStrategy1 : ISearchStrategy
{
    private const int AlgorithmVersion = 3;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 6);
        queryBuilder.AddExactGfd();
        queryBuilder.AddExactAll();
        queryBuilder.AddFuzzyGfd();
        queryBuilder.AddFuzzyAll();
        queryBuilder.AddFuzzyGfdRange();
        queryBuilder.TryAddFuzzyAltDob();

        return queryBuilder.Build();
    }

    public int GetAlgorithmVersion()
    {
        return AlgorithmVersion;
    }
}