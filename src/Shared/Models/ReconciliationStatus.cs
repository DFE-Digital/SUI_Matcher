namespace Shared.Models;

public enum ReconciliationStatus
{
    LocalDemographicsDidNotMatchToAnNhsNumber,
    LocalNhsNumberIsNotValid,
    LocalNhsNumberIsNotFoundInNhs,
    LocalNhsNumberIsSuperseded,
    NoDifferences,
    Differences,
    Error, // For any unidentified errors, not covered by the above
}