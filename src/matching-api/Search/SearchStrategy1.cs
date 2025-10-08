using Shared;
using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Quite a broad strategy that uses a variety of exact and fuzzy searches with different combinations of parameters.
/// </summary>
public class SearchStrategy1 : ISearchStrategy
{
    private int AlgorithmVersion { get; }
    private static readonly IReadOnlyCollection<int?> AllVersions = [1, 2, 3];

    public SearchStrategy1(int? version = null)
    {
        AlgorithmVersion = version ?? 3;
        if (!AllVersions.Contains(AlgorithmVersion))
            throw new ArgumentOutOfRangeException(nameof(version),
                $"Version {version} not supported for {nameof(SearchStrategy1)}");
    }

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

    public IReadOnlyCollection<int?> GetAllAlgorithmVersions()
    {
        return AllVersions;
    }
}