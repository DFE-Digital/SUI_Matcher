using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Evolving strategy, iteratively improved based on performance and feedback.
/// </summary>
public class SearchStrategy3 : ISearchStrategy
{
    private const int AlgorithmVersion = 5;
    // Results so far
    // V2 = Only 1 record difference to V1 but no additional Matches
    // V3 = Only 1 record difference to V1 and V2 but no additional Matches
    // V4 = 20-25%% increase in Matches

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 6);

        return VersionFactory(AlgorithmVersion, queryBuilder);
    }
    
    private OrderedDictionary<string, SearchQuery> VersionFactory(int version, SearchQueryBuilder queryBuilder)
    {
        return version switch
        {
            1 => Version1(queryBuilder),
            2 => Version2(queryBuilder),
            3 => Version3(queryBuilder),
            4 => Version4(queryBuilder),
            5 => Version5(queryBuilder),
            _ => throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported version: {version}")
        };
    }
    
    private static OrderedDictionary<string, SearchQuery> Version1(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version2(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyAllPostcodeWildcard();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version3(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyAllPostcodeWildcard();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyGfdPostcodeWildcard();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version4(SearchQueryBuilder queryBuilder)
    {
        // Testing that adding a DOB range non-fuzzy GFD at the start helps
        queryBuilder.AddNonFuzzyGfdRange();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }

    private static OrderedDictionary<string, SearchQuery> Version5(SearchQueryBuilder queryBuilder)
    {
        // Testing that adding a DOB range fuzzy GFD at the start helps
        queryBuilder.AddFuzzyGfdRange();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }

    public int GetAlgorithmVersion()
    {
        return AlgorithmVersion;
    }
}