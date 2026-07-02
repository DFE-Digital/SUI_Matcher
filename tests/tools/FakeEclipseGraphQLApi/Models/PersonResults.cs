namespace FakeEclipseGraphQLApi.Models;

public class PersonResults
{
    public ResultsCursor? Cursor { get; set; }
    public List<Person>? Results { get; set; }
}