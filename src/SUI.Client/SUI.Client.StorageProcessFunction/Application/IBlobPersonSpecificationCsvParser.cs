using Shared.Models;

namespace SUI.StorageProcessFunction.Application;

public interface IBlobPersonSpecificationCsvParser
{
    Task<IReadOnlyList<PersonSpecification>> ParseAsync(
        BlobFileContent blobFile,
        CancellationToken cancellationToken
    );
}
