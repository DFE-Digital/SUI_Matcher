using Shared.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public class AddressComparisonResults
{
    public AddressComparisonResult PrimaryAddressSame { get; set; } =
        new(AddressComparisonResult.AddressMatchStatus.None);
    public AddressComparisonResult AddressHistoriesIntersect { get; set; } =
        new(AddressComparisonResult.AddressMatchStatus.None);
    public AddressComparisonResult PrimaryCMSAddressInPDSHistory { get; set; } =
        new(AddressComparisonResult.AddressMatchStatus.None);
    public AddressComparisonResult PrimaryPDSAddressInCMSHistory { get; set; } =
        new(AddressComparisonResult.AddressMatchStatus.None);
}

public sealed record AddressComparisonResult(
    AddressComparisonResult.AddressMatchStatus Status,
    AddressComparisonResult.AddressMatchReason Reason =
        AddressComparisonResult.AddressMatchReason.None
)
{
    public string GetResultMessage()
    {
        return Status switch
        {
            AddressMatchStatus.Matched => "Matched",
            AddressMatchStatus.Unmatched => "Unmatched",
            AddressMatchStatus.Uncertain => $"Uncertain-{Reason.ToString()}",
            AddressMatchStatus.None => "NoComparison",
            _ => $"Unknown status: {Status}",
        };
    }

    public enum AddressMatchStatus
    {
        None,
        Matched,
        Unmatched,
        Uncertain,
    }

    public enum AddressMatchReason
    {
        None,
        PostcodeMismatch,
        BuildingNumberMissing,
        NumberRange,
        FlatMissing,
    }
}
