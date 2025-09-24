using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Non-Fuzzy and Fuzzy search with different combinations of parameters.
/// Includes DOB range in non-fuzzy search and postcode in fuzzy search.
/// </summary>
public class SearchStrategy2 : ISearchStrategy
{
    private const int AlgorithmVersion = 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model);
        queryBuilder.AddNonFuzzyGfd();
        queryBuilder.AddNonFuzzyGfdRange();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyGfd();
        queryBuilder.AddFuzzyGfdRangePostcode();
        queryBuilder.AddFuzzyAll(); 
        queryBuilder.TryAddFuzzyAltDob(); 
        
        return queryBuilder.Build();
    }

    public int GetAlgorithmVersion()
    {
        return AlgorithmVersion;
    }
}