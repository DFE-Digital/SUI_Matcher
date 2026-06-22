namespace SUI.Client.Core.Application.Interfaces;

public interface IReconciliationDataParser<TSource>
{
    ReconciliationSourceData Parse(TSource record);
}

public sealed record ReconciliationSourceData(string? NhsNumber, string? AddressHistory);
