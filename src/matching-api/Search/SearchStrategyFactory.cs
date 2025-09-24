using Shared;

namespace MatchingApi.Search;

public static class SearchStrategyFactory
{
    private static readonly Dictionary<string, ISearchStrategy> Strategies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SharedConstants.SearchStrategy.Strategies.Strategy1, new SearchStrategy1() },
            { SharedConstants.SearchStrategy.Strategies.Strategy2, new SearchStrategy2() },
            { SharedConstants.SearchStrategy.Strategies.Strategy3, new SearchStrategy3() },
        };

    public static ISearchStrategy Get(string name)
    {
        if (!Strategies.TryGetValue(name, out var strategy))
            throw new ArgumentException($"Unknown strategy '{name}'");

        return strategy;
    }
}