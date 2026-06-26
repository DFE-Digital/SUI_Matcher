using SUI.Client.Core.Domain.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public interface ISourceAddressHistoryParser
{
    AddressHistory Parse(string? historyString, string? primaryPostcode);
}
