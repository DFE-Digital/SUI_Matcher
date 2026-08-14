using ExternalApi;
using ExternalApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Unit.Tests.External.Services;

public class FhirClientFactoryTests
{
    [Fact]
    public void Should_AddOdsCodeHeader_When_OdsCodeIsConfigured()
    {
        var factory = CreateFhirClientFactory("A313");

        var fhirClient = factory.CreateFhirClient();

        Assert.NotNull(fhirClient.RequestHeaders);
        Assert.True(fhirClient.RequestHeaders.TryGetValues("odsCode", out var values));
        Assert.Equal("A313", Assert.Single(values));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_NotAddOdsCodeHeader_When_OdsCodeIsNotConfigured(string? odsCode)
    {
        var factory = CreateFhirClientFactory(odsCode);

        var fhirClient = factory.CreateFhirClient();

        Assert.NotNull(fhirClient.RequestHeaders);
        Assert.False(fhirClient.RequestHeaders.Contains("odsCode"));
    }

    [Fact]
    public void Should_PreserveAuthorizationHeader_When_OdsCodeIsConfigured()
    {
        var factory = CreateFhirClientFactory("A313");

        var fhirClient = factory.CreateFhirClient();

        Assert.NotNull(fhirClient.RequestHeaders);
        Assert.Equal("Bearer", fhirClient.RequestHeaders.Authorization?.Scheme);
        Assert.Equal("test-token", fhirClient.RequestHeaders.Authorization?.Parameter);
    }

    private static FhirClientFactory CreateFhirClientFactory(string? odsCode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"] = "https://fhir.api.endpoint",
                }
            )
            .Build();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.GetBearerToken()).ReturnsAsync("test-token");

        return new FhirClientFactory(
            Mock.Of<ILogger<FhirClientFactory>>(),
            tokenService.Object,
            configuration,
            Options.Create(new NhsFhirConfigOptions { OdsCode = odsCode })
        );
    }
}
