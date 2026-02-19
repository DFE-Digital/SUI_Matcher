using MatchingApi.Exceptions;

using Shared;
using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Strategy 5 introduces a theory that by removing the first name from the search, we can increase match rate
/// </summary>
public class SearchStrategy5 : ISearchStrategy
{
    private int AlgorithmVersion { get; }
    private static readonly IReadOnlyCollection<int?> AllVersions = [1];

    public SearchStrategy5(int? version = null)
    {
        AlgorithmVersion = version ?? 1;
        if (!AllVersions.Contains(AlgorithmVersion))
            throw new InvalidStrategyException(
                $"{SharedConstants.SearchStrategy.VersionErrorMessagePrefix} ({version}) For strategy ({SharedConstants.SearchStrategy.Strategies.Strategy5})");
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
            _ => throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported version: {version}")
        };
    }

    /// <summary>
    /// Version 1 replicates Strategy4 Version 2 and adds two additional queries which remove the first name from the search.
    /// </summary>
    /// <param name="queryBuilder"></param>
    /// <returns></returns>
    private static OrderedDictionary<string, SearchQuery> Version1(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyGfd();

        queryBuilder.AddFuzzyGfd();
        
        queryBuilder.AddFuzzyFdgPostcode();
        queryBuilder.AddFuzzyFdPostcode();

        queryBuilder.AddFuzzyAll();

        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);

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