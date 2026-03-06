namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public class AddressComparisonResult
{
    public bool PrimaryAddressSame { get; set; }
    public bool AddressHistoriesIntersect { get; set; }
    public bool PrimaryCMSAddressInPDSHistory { get; set; }
    public bool PrimaryPDSAddressInCMSHistory { get; set; }
}