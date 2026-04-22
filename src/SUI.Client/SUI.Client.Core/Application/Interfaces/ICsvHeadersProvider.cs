namespace SUI.Client.Core.Application.Interfaces;

public interface ICsvHeadersProvider
{
    IReadOnlyCollection<string> GetRequiredHeaders();
}
