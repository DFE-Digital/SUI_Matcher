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
    private static readonly IReadOnlyCollection<int?> AllVersions = [1];

    public SearchStrategy4(int? version = null)
    {
        AlgorithmVersion = version ?? 1;
        if (!AllVersions.Contains(AlgorithmVersion))
            throw new InvalidStrategyException(
                $"{SharedConstants.SearchStrategy.VersionErrorMessagePrefix} ({version}) For strategy ({SharedConstants.SearchStrategy.Strategies.Strategy4})");
    }

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
            _ => throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported version: {version}")
        };
    }

    private static OrderedDictionary<string, SearchQuery> Version1(SearchQueryBuilder queryBuilder)
    {
        queryBuilder.AddNonFuzzyGfd(preprocessNames: true);

        queryBuilder.AddFuzzyGfd(preprocessNames: true);

        queryBuilder.AddFuzzyAll(preprocessNames: true);

        queryBuilder.AddNonFuzzyGfdRange(preprocessNames: true);

        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false, preprocessNames: true);

        queryBuilder.AddFuzzyGfdRange(preprocessNames: true);

        queryBuilder.AddFuzzyGfdRangePostcode(preprocessNames: true);

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