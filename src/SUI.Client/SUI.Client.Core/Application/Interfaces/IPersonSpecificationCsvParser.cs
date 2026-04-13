using Shared.Models;

namespace SUI.Client.Core.Application.Interfaces;

public interface IPersonSpecificationCsvParser
{
    List<PersonSpecification> ParseListAsync(
        BinaryData content,
        string fileName,
        CancellationToken cancellationToken
    );
}
