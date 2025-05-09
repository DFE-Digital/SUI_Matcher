namespace FakeNhsFhirApi;

public class FakeItem
{
    public required string MatchType { get; set; }
    public FakePerson Person { get; set; } = new();
    public string ResponseJson { get; set; } = null!;
    public double Score { get; set; }
}