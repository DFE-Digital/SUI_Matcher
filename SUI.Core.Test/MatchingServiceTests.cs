using Moq;
using SUI.Core.Domain;
using SUI.Core.Endpoints;
using SUI.Core.Services;

namespace SUI.Core.Test;

[TestClass]
public sealed class MatchingServiceTests
{
    [TestMethod]
    public async Task TestMatchingDemo1()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(nhsFhir.Object, new ValidationService());

        var result = await subj.SearchAsync(new PersonSpecification());

        Assert.IsNotNull(result);
    }
}
