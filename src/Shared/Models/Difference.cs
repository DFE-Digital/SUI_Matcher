namespace Shared.Models;

public class Difference
{
    public required string FieldName { get; set; }

    public string? Local { get; set; }

    public string? Nhs { get; set; }
}