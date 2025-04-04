namespace Shared.Models;

public class PatientDemographicResult
{
    // Passing through the entire response from the NHS API at the moment. This may change in the future.
    public dynamic? Result { get; set; }
    public string? ErrorMessage { get; set; }
}