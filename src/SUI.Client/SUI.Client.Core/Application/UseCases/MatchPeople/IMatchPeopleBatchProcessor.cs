using Shared.Models;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public interface IMatchPeopleBatchProcessor
{
    Task<MatchingProcessStats> ProcessAsync(
        ProcessPersonBatchRequest request,
        CancellationToken cancellationToken
    );
}
