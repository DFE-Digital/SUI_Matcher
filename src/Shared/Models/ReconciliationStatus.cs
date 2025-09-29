namespace Shared.Models;

public enum ReconciliationStatus
{
    NoDifferences,
    ManyDifferences,
    OneDifference,
    SupersededNhsNumber,
    MissingNhsNumber,
    InvalidNhsNumber,
    PatientNotFound,
    Error
}