namespace SUI.Client.Core.Application.Interfaces;

public interface IReconciliationDataParser<in TSource>
{
    ReconciliationSourceData Parse(TSource record);
}

public sealed record ReconciliationSourceData(string? NhsNumber, string? AddressHistory);
