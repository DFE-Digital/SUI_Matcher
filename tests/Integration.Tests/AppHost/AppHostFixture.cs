using AppHost.SwaggerUi;

using Aspire.Hosting.Testing;

using FluentAssertions.Primitives;

using WireMock.Admin.Requests;
using WireMock.Client;

namespace Integration.Tests.AppHost;

public sealed class AppHostFixture() : DistributedApplicationFactory(typeof(Projects.AppHost)), IAsyncLifetime
{
    private DistributedApplication? _app;
    private IResourceBuilder<WireMockServerResource> NhsAuthMockService { get; set; } = null!;

    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        applicationOptions.AssemblyName = typeof(Projects.AppHost).Assembly.FullName;
        applicationOptions.DisableDashboard = true;
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        NhsAuthMockService = applicationBuilder.AddWireMock("mock-auth-api", WireMockServerArguments.DefaultPort)
            .WithApiMappingBuilder(MockNhsFhirServer.SetupAsync);
    }

    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        var resources = applicationBuilder.Resources
            .Where(res => (res.Name == "external-api") || res.Name == "matching-api" || res.Name == "yarp")
            .OfType<ProjectResource>()
            .ToArray();

        foreach (var resource in resources)
        {
            applicationBuilder.Resources.Remove(resource);
        }

        var externalApi = applicationBuilder.AddProject<Projects.External>("external-api")
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"] = $"{NhsAuthMockService.GetEndpoint("http").Url}/personal-demographics/FHIR/R4/";
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"] = $"{NhsAuthMockService.GetEndpoint("http").Url}/oauth2/token";
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES"] = "1";
            })
            .WaitFor(NhsAuthMockService);

        var matchingApi = applicationBuilder.AddProject<Projects.Matching>("matching-api")
            .WithReference(externalApi)
            .WithSwaggerUi().WaitFor(NhsAuthMockService);

        applicationBuilder.AddProject<Projects.Yarp>("yarp")
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["ConnectionStrings:secrets"] = "http://localhost:8080";
            })
            .WithReference(matchingApi).WaitFor(matchingApi).WaitFor(NhsAuthMockService);
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        _app = application;
    }

    public async Task InitializeAsync()
    {
        await StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    public IWireMockAdminApi NhsAuthMockApi()
    {
        return _app!.CreateWireMockAdminClient("mock-auth-api");
    }
}

public static class WireMockExtensions
{
    public static async Task<WireMockReceivedAssertions> Should(this IWireMockAdminApi instance)
    {
        return new WireMockReceivedAssertions(await instance.GetRequestsAsync());
    }
}

public class WireMockReceivedAssertions(IList<LogEntryModel> logEntryModels)
    : ReferenceTypeAssertions<IList<LogEntryModel>, WireMockReceivedAssertions>(logEntryModels)
{
    public WireMockAssertions HaveReceivedNoCalls()
    {
        return new WireMockAssertions(Subject, 0);
    }

    public WireMockAssertions HaveReceivedACall()
    {
        return new WireMockAssertions(Subject, null);
    }

    public WireMockANumberOfCallsAssertions HaveReceived(int callsCount)
    {
        return new WireMockANumberOfCallsAssertions(Subject, callsCount);
    }

    protected override string Identifier => "wiremockadminapi";
}

public class WireMockAssertions(IList<LogEntryModel> logEntryModels, int? callsCount = 1)
{
    public void AtPath(string path)
    {
        var actualCount = logEntryModels.Count(entry => entry.Request.AbsolutePath == path);

        Assert.True(callsCount == actualCount,
            $"For path '{path}' there were {actualCount} calls, instead of the expected {callsCount}");
    }
}

public class WireMockANumberOfCallsAssertions(IList<LogEntryModel> logEntryModels, int callsCount)
{
    public WireMockAssertions Calls()
    {
        return new WireMockAssertions(logEntryModels, callsCount);
    }
}