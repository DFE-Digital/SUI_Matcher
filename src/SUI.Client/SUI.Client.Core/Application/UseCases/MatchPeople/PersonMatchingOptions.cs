using Shared;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public sealed class PersonMatchingOptions
{
    public const string SectionName = "PersonMatching";

    public string SearchStrategy { get; init; } =
        SharedConstants.SearchStrategy.Strategies.Strategy4;

    public int? StrategyVersion { get; init; } = 2;
}
