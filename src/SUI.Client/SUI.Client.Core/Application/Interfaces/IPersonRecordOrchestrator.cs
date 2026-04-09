using Shared.Models;

namespace SUI.Client.Core.Application.Interfaces;

public interface IPersonRecordOrchestrator<TSource>
{
    Task<
        List<SUI.Client.Core.Application.UseCases.MatchPeople.ProcessedRecord<TSource>>
    > ProcessAsync(
        IEnumerable<TSource> content,
        string fileName,
        CancellationToken cancellationToken
    );
}
