using Shared.Models;

namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IPersonSpecificationCsvParser
{
    List<PersonSpecification> ParseListAsync(
        BinaryData content,
        string fileName,
        CancellationToken cancellationToken
    );
}
