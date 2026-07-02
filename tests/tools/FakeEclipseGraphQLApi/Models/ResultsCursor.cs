namespace FakeEclipseGraphQLApi.Models;

public class ResultsCursor
{
    public long Offset { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int Returned { get; set; }
    public long TotalSize { get; set; }
}