using Shared.Models;

namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IPersonSpecificationCsvParser
{
    IAsyncEnumerable<PersonSpecification> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken
    );
}
