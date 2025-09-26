using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Evolving strategy, iteratively improved based on performance and feedback.
/// </summary>
public class SearchStrategy3 : ISearchStrategy
{
    private const int AlgorithmVersion = 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 6);
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();

        return queryBuilder.Build();
    }

    public int GetAlgorithmVersion()
    {
        return AlgorithmVersion;
    }
}