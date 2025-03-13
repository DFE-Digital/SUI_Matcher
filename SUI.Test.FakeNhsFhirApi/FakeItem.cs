namespace SUI.Test.FakeNhsFhirApi;

public class FakeItem
{
    public string MatchType { get; set; }
    public FakePerson Person { get; set; } = new();
    public string ResponseJson { get; set; }
    public double Score { get; set; }
}
