using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Non fuzzy and fuzzy searches with a smaller DOB range of 1 month
/// </summary>
public class SearchStrategy3 : ISearchStrategy
{
    private const int AlgorithmVersion = 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        // Reduce dob range to 1
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 1);
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