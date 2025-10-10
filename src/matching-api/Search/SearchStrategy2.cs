using MatchingApi.Exceptions;

using Shared;
using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Non-Fuzzy and Fuzzy search with different combinations of parameters.
/// Includes DOB range in non-fuzzy search and postcode in fuzzy search.
/// </summary>
public class SearchStrategy2 : ISearchStrategy
{
    private int AlgorithmVersion { get; }
    private static readonly IReadOnlyCollection<int?> AllVersions = [1];

    public SearchStrategy2(int? version = null)
    {
        AlgorithmVersion = version ?? 1;
        if (!AllVersions.Contains(AlgorithmVersion))
            throw new InvalidStrategyException(
                $"{SharedConstants.SearchStrategy.VersionErrorMessagePrefix} ({version}) For strategy ({SharedConstants.SearchStrategy.Strategies.Strategy2})");
    }

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model);
        queryBuilder.AddNonFuzzyGfd(); // 1
        queryBuilder.AddNonFuzzyGfdRange(); // 4
        queryBuilder.AddNonFuzzyAllPostcodeWildcard();
        queryBuilder.AddNonFuzzyAll(); // 6
        queryBuilder.AddFuzzyGfd(); // 2
        queryBuilder.AddFuzzyGfdRangePostcodeWildcard();
        queryBuilder.AddFuzzyGfdRangePostcode(); // 5
        queryBuilder.AddFuzzyAll(); // 3
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