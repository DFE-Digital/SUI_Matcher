namespace SUI.Client.Core.Application.Interfaces;

public interface ICsvRequiredHeadersProvider
{
    IReadOnlyCollection<string> GetRequiredHeaders();
}
