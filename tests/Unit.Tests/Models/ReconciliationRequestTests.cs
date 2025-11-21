using Shared.Models;

namespace Unit.Tests.Models;

public class ReconciliationRequestTests
{
    [Fact]
    public void ReconciliationId_ComputesCorrectly()
    {
        var request = new ReconciliationRequest()
        {
            NhsNumber = "9999999993",
            Given = "David",
            Family = "Smith",
            BirthDate = new DateOnly(2000, 1, 1),
            Gender = "Male",
            AddressPostalCode = "A2 7CB",
            Email = "david.smith@example.com",
            Phone = "123454321"
        };

        // All above fields, in that order, separated by '|', encoded as UTF8 and put through SHA256
        Assert.Equal("3C962EF930C5FE6F961BDA338FA6DFBE4FB2ACAC0E5D2A5D72B8D42442F852CC", request.ReconciliationId);
    }
}