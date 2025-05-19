using System.Diagnostics;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

using Moq;

using Shared.Logging;

namespace Unit.Tests.Logging;

[TestClass]
public class ApplicationEnricherTests
{
    [TestMethod]
    public void Enrich_AddsSearchIdToCollector_WhenSearchIdExistsInActivity()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor();
        var collector = new Mock<IEnrichmentTagCollector>();
        var enricher = new ApplicationEnricher(httpContextAccessor);
        var activity = new Activity("TestActivity");
        activity.AddBaggage("SearchId", "12345");
        activity.Start();

        // Act
        enricher.Enrich(collector.Object);

        // Assert
        collector.Verify(c => c.Add("SearchId", "12345"), Times.Once);

        activity.Stop();
    }

    [TestMethod]
    public void Enrich_AddsMachineNameToCollector()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor();
        var collector = new Mock<IEnrichmentTagCollector>();
        var enricher = new ApplicationEnricher(httpContextAccessor);

        // Act
        enricher.Enrich(collector.Object);

        // Assert
        collector.Verify(c => c.Add("MachineName", Environment.MachineName), Times.Once);
    }

    [TestMethod]
    public void Enrich_AddsIsAuthenticatedToCollector_WhenHttpContextExists()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "TestAuth"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var collector = new Mock<IEnrichmentTagCollector>();
        var enricher = new ApplicationEnricher(httpContextAccessor);

        // Act
        enricher.Enrich(collector.Object);

        // Assert
        collector.Verify(c => c.Add("IsAuthenticated", true), Times.Once);
    }

    [TestMethod]
    public void Enrich_DoesNotAddIsAuthenticated_WhenHttpContextIsNull()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var collector = new Mock<IEnrichmentTagCollector>();
        var enricher = new ApplicationEnricher(httpContextAccessor);

        // Act
        enricher.Enrich(collector.Object);

        // Assert
        collector.Verify(c => c.Add("IsAuthenticated", It.IsAny<bool>()), Times.Never);
    }
}