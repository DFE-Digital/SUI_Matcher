using Shared.Models;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public sealed class ProcessPersonBatchRequest
{
    public required IReadOnlyList<PersonSpecification> People { get; init; }

    public required string SearchStrategy { get; init; }

    public int? StrategyVersion { get; init; }

    public string? BatchIdentifier { get; init; }
}
