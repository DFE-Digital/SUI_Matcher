namespace Shared.Models;

public class DemographicResult
{
    public NhsPerson? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public Status Status { get; set; }
}

public enum Status
{
    Success,
    InvalidNhsNumber,
    PatientNotFound,
    Error
}