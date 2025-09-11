namespace Shared.Models;

public enum ReconciliationStatus
{
    NoDifferences,
    ManyDifferences,
    OneDifference,
    SupersededNhsNumber,
    Error
}