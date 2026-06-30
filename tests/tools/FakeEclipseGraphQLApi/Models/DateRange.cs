namespace FakeEclipseGraphQLApi.Models;

public class DateRange
{
    public DateOnly? Lower { get; set; }
    public DateOnly? Upper { get; set; }
    public string? Mask { get; set; }
}