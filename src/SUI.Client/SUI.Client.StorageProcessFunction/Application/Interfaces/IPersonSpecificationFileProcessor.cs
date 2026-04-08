namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IPersonSpecificationFileOrchestrator
{
    Task ProcessAsync(Stream content, string fileName, CancellationToken cancellationToken);
}
