using Shared;

namespace MatchingApi.Search;

public static class SearchStrategyFactory
{
    private static readonly Dictionary<string, Func<int?, ISearchStrategy>> Strategies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SharedConstants.SearchStrategy.Strategies.Strategy1, v => new SearchStrategy1(v) },
            { SharedConstants.SearchStrategy.Strategies.Strategy2, v => new SearchStrategy2(v) },
            { SharedConstants.SearchStrategy.Strategies.Strategy3, v => new SearchStrategy3(v) },
            { SharedConstants.SearchStrategy.Strategies.Strategy4, v => new SearchStrategy4(v) },
        };

    public static ISearchStrategy Get(string name, int? version = null)
    {
        if (!Strategies.TryGetValue(name, out var factory))
            throw new ArgumentException($"Unknown strategy '{name}'");

        return factory(version);
    }
}