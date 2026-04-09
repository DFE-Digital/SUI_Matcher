using Shared.Models;

namespace SUI.Client.Core.Application.Interfaces;

public interface IMatchPersonRecordOrchestrator<TSource>
{
    Task<
        List<SUI.Client.Core.Application.UseCases.MatchPeople.ProcessedMatchRecord<TSource>>
    > ProcessAsync(
        IEnumerable<TSource> content,
        string fileName,
        CancellationToken cancellationToken
    );
}
