using MatchingApi.Exceptions;

using Shared;
using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Strategy 3 but with processing to split given name into a list and remove bracketed names from family name
/// </summary>
public class SearchStrategy4 : ISearchStrategy
{
    private int AlgorithmVersion { get; }
    private static readonly IReadOnlyCollection<int?> AllVersions = [1, 2];

    public SearchStrategy4(int? version = null)
    {
        AlgorithmVersion = version ?? 1;
        if (!AllVersions.Contains(AlgorithmVersion))
            throw new InvalidStrategyException(
                $"{SharedConstants.SearchStrategy.VersionErrorMessagePrefix} ({version}) For strategy ({SharedConstants.SearchStrategy.Strategies.Strategy4})");
    }

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 6, preprocessNames: true);

        return VersionFactory(AlgorithmVersion, queryBuilder);
    }

    private static OrderedDictionary<string, SearchQuery> VersionFactory(int version, SearchQueryBuilder queryBuilder)
    {
        return version switch
        {
            1 => Version1(queryBuilder),
            2 => Version2(queryBuilder),
            _ => throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported version: {version}")
        };
    }

    private static OrderedDictionary<string, SearchQuery> Version1(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyGfd();

        queryBuilder.AddFuzzyGfd();

        queryBuilder.AddFuzzyAll();

        queryBuilder.AddNonFuzzyGfdRange();

        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);

        queryBuilder.AddFuzzyGfdRange();

        queryBuilder.AddFuzzyGfdRangePostcode();

        return queryBuilder.Build();
    }

    /// <summary>
    ///  Version 2 removes the non-fuzzy and fuzzy GFD range searches compared to V1, to test the impact of removing these searches on matching performance
    /// </summary>
    /// <param name="queryBuilder"></param>
    /// <returns></returns>
    private static OrderedDictionary<string, SearchQuery> Version2(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyGfd();

        queryBuilder.AddFuzzyGfd();

        queryBuilder.AddFuzzyAll();

        // queryBuilder.AddNonFuzzyGfdRange(); // Removal is intentional to test impact of removing this search

        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);

        // queryBuilder.AddFuzzyGfdRange(); // Removal is intentional to test impact of removing this search

        queryBuilder.AddFuzzyGfdRangePostcode();

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