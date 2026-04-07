namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IPersonSpecificationFileProcessor
{
    Task ProcessAsync(Stream content, string fileName, CancellationToken cancellationToken);
}
