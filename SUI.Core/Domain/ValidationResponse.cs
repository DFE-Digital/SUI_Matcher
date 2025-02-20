namespace SUI.Core.Domain;

public class ValidationResponse
{
    public IEnumerable<ValidationResult>? Results { get; set; }

    public class ValidationResult
    {
        public IEnumerable<string> MemberNames { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
