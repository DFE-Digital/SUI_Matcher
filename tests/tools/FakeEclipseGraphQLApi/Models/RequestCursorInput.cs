namespace FakeEclipseGraphQLApi.Models;

public class RequestCursorInput
{
    public int? Offset { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}