using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Evolving strategy, iteratively improved based on performance and feedback.
/// </summary>
public class SearchStrategy3 : ISearchStrategy
{
    private const int AlgorithmVersion = 13;
    // Results so far
    // V2 = Only 1 record difference to V1 but no additional Matches
    // V3 = Only 1 record difference to V1 and V2 but no additional Matches
    // V4 = 20-25%% increase in Matches

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 6);

        return VersionFactory(AlgorithmVersion, queryBuilder);
    }
    
    private static OrderedDictionary<string, SearchQuery> VersionFactory(int version, SearchQueryBuilder queryBuilder)
    {
        return version switch
        {
            1 => Version1(queryBuilder),
            2 => Version2(queryBuilder),
            3 => Version3(queryBuilder),
            4 => Version4(queryBuilder),
            5 => Version5(queryBuilder),
            6 => Version6(queryBuilder),
            7 => Version7(queryBuilder),
            8 => Version8(queryBuilder),
            9 => Version9(queryBuilder),
            10 => Version10(queryBuilder),
            11 => Version11(queryBuilder),
            12 => Version12(queryBuilder),
            13 => Version13(queryBuilder),
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
    
    private static OrderedDictionary<string, SearchQuery> Version6(SearchQueryBuilder queryBuilder)
    {
        // Non Fuzzy dob range with postcode
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version7(SearchQueryBuilder queryBuilder)
    {
        // Non Fuzzy dob range with postcode wildcard
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: true);
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version8(SearchQueryBuilder queryBuilder)
    {
        // Non Fuzzy dob range with postcode
        queryBuilder.AddFuzzyGfdRangePostcode();
        queryBuilder.AddFuzzyGfdRangePostcodeWildcard();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version9(SearchQueryBuilder queryBuilder)
    {
        // Combine and see results vs V1
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: true);
        queryBuilder.AddFuzzyGfdRangePostcode();
        queryBuilder.AddFuzzyGfdRangePostcodeWildcard();
        queryBuilder.AddNonFuzzyAll();
        queryBuilder.AddFuzzyAll();
        return queryBuilder.Build();
    }

    private static OrderedDictionary<string, SearchQuery> Version10(SearchQueryBuilder queryBuilder)
    {
        // Precision first, fuzzy and range later
        
        queryBuilder.AddNonFuzzyGfd(); // 1
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false); // 4
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: true); // ? didn't show up in logs
        queryBuilder.AddNonFuzzyAll(); // 6
        
        // Fuzzy next
        queryBuilder.AddFuzzyGfd(); // 2
        queryBuilder.AddFuzzyGfdRangePostcode(); // 5
        queryBuilder.AddFuzzyGfdRangePostcodeWildcard(); // 7
        queryBuilder.AddFuzzyAll(); // 3
        
        return queryBuilder.Build();
    }
    
    // V11 will use a Dob range for first non-fuzzy GFD
    private static OrderedDictionary<string, SearchQuery> Version11(SearchQueryBuilder queryBuilder)
    {
        // Ordered strictly by observed performance from v1-v10 v10, with AddNonFuzzyGfdRange added back in as it did well in v4

        queryBuilder.AddNonFuzzyGfd(); 
    
        queryBuilder.AddNonFuzzyGfdRange();

        queryBuilder.AddFuzzyGfd(); 
        
        queryBuilder.AddFuzzyAll(); 
        
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false); 
    
        queryBuilder.AddFuzzyGfdRangePostcode(); 
    
        queryBuilder.AddNonFuzzyAll(); 

        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version12(SearchQueryBuilder queryBuilder)
    {
        // Ordered strictly by observed performance from v11 - order by most matches found
        // Direct comparison of V11 to see if changing order makes a difference

        queryBuilder.AddNonFuzzyGfd(); 
        
        queryBuilder.AddFuzzyGfd(); 
        
        queryBuilder.AddFuzzyAll(); 
    
        queryBuilder.AddNonFuzzyGfdRange();
        
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false); 
    
        queryBuilder.AddFuzzyGfdRangePostcode(); 
    
        queryBuilder.AddNonFuzzyAll(); // <- got no results at all in v11. Shows that the other queries pick up everything this would have found.

        return queryBuilder.Build();
    }
    
    // V13 will contain AddFuzzyGfdRange() and the more optimal ordering based on V11 and V12 results
    
    private static OrderedDictionary<string, SearchQuery> Version13(SearchQueryBuilder queryBuilder)
    {
        // Ordered strictly by observed performance from v12 - order by most matches found
        // Removed NonFuzzyAll as it found no additional records in V11 and V12
        // Added AddFuzzyGfdRange() to see how it effects things

        queryBuilder.AddNonFuzzyGfd(); 
        
        queryBuilder.AddFuzzyGfd(); 
        
        queryBuilder.AddFuzzyAll(); 
    
        queryBuilder.AddNonFuzzyGfdRange();
        
        queryBuilder.AddFuzzyGfdRange(); // New addition
        
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false); 
    
        queryBuilder.AddFuzzyGfdRangePostcode(); 

        return queryBuilder.Build();
    }
    
    private static OrderedDictionary<string, SearchQuery> Version14(SearchQueryBuilder queryBuilder)
    {
        // Ordered strictly by observed performance from v13 - order by most matches found
        // Moved AddFuzzyGfdRange down as it found less than NonFuzzyGfdRangePostcode

        queryBuilder.AddNonFuzzyGfd(); 
        
        queryBuilder.AddFuzzyGfd(); 
        
        queryBuilder.AddFuzzyAll(); 
    
        queryBuilder.AddNonFuzzyGfdRange();
        
        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false); 
        
        queryBuilder.AddFuzzyGfdRange();
    
        queryBuilder.AddFuzzyGfdRangePostcode();

        return queryBuilder.Build();
    }
    
    // Consider: Final version will contain wildcards just to see if they help at all
    // To ponder on: Should we try and swap day and month round? it shows that there is a data issue and should be investigated.

    public int GetAlgorithmVersion()
    {
        return AlgorithmVersion;
    }
}