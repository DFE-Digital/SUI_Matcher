using Aspire.Hosting.Testing;
using Xunit.Abstractions;

namespace AppHost.IntegrationTests;

public class AppHostIntegrationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> GetEndpoints()
    {
        yield return new object[] { "matching-api", "/health" };
        yield return new object[] { "external-api", "/health" };
    }

    [Fact]
    public async Task AppHostRunsCleanly()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(TimeSpan.FromSeconds(15));

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(120));
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await resourceNotificationService.WaitForResourceAsync("yarp", KnownResourceStates.Running)
                                         .WaitAsync(TimeSpan.FromSeconds(180));


        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(15));
    }

    [Theory]
    [MemberData(nameof(GetEndpoints))]
    public async Task AppHostApiChecks(string endpointName, string endpointUrl)
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(TimeSpan.FromSeconds(15));

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(120));
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();

        using var httpClient = app.CreateHttpClient(endpointName);
        await resourceNotificationService.WaitForResourceAsync(
            endpointName,
            KnownResourceStates.Running
            )
            .WaitAsync(TimeSpan.FromSeconds(30));

        var response = await httpClient.GetAsync(endpointUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(15));
    }

}
