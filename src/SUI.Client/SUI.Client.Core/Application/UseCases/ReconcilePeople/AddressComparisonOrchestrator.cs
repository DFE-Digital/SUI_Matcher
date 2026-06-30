using Shared.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public sealed class AddressComparisonOrchestrator(
    ISourceAddressHistoryParser sourceAddressHistoryParser
)
{
    public AddressComparisonResults GetAddressComparisonResult(
        ReconciliationRequest request,
        ReconciliationResponse? response,
        string? addressHistoryCsv
    )
    {
        var result = new AddressComparisonResults();

        if (response?.Person == null)
        {
            return result;
        }

        var pdsAddressHistory = AddressParser.FromNhsPerson(response.Person);
        var queryingAddressHistory = sourceAddressHistoryParser.Parse(
            addressHistoryCsv,
            request.AddressPostalCode
        );

        result.PrimaryAddressSame = pdsAddressHistory.PrimaryAddressSameAs(queryingAddressHistory);
        result.AddressHistoriesIntersect = pdsAddressHistory.IntersectsWith(queryingAddressHistory);

        result.PrimaryCMSAddressInPDSHistory = queryingAddressHistory.PrimaryAddressInHistoryOf(
            pdsAddressHistory
        );
        result.PrimaryPDSAddressInCMSHistory = pdsAddressHistory.PrimaryAddressInHistoryOf(
            queryingAddressHistory
        );

        return result;
    }
}
