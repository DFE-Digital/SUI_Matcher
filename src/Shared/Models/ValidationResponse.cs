namespace Shared.Models;

public class ValidationResponse
{
    public IEnumerable<ValidationResult>? Results { get; init; }

    public class ValidationResult
    {
        public required IEnumerable<string> MemberNames { get; init; } = [];
        public string? ErrorMessage { get; init; }
    }
}