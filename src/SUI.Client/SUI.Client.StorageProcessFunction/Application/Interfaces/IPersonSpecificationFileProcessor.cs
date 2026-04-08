using Shared.Models;

namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IPersonRecordOrchestrator
{
    Task ProcessAsync(
        List<PersonSpecification> content,
        string fileName,
        CancellationToken cancellationToken
    );
}
