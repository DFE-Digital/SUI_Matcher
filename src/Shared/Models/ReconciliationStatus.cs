namespace Shared.Models;

public enum ReconciliationStatus
{
    NoDifferences,
    Differences,
    SupersededNhsNumber,
    MissingNhsNumber,
    InvalidNhsNumber,
    PatientNotFound,
    Error
}