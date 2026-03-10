namespace Shared.Models;

public class Difference
{
    public required string FieldName { get; set; }

    public string? Local { get; set; }

    public string? Nhs { get; set; }

    public bool BothSidesPresentAndDifferent => !string.IsNullOrEmpty(Local) && !string.IsNullOrEmpty(Nhs) && !Local.Equals(Nhs, StringComparison.OrdinalIgnoreCase);
    public bool IsMissingNhs => string.IsNullOrEmpty(Nhs);
}