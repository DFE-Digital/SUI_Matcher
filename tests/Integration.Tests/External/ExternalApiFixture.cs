using AppHost.SwaggerUi;

using Aspire.Hosting.Testing;

namespace Integration.Tests.External;

public sealed class ExternalApiFixture() : DistributedApplicationFactory(typeof(Projects.External)), IAsyncLifetime
{
    private DistributedApplication? _app;
    public IResourceBuilder<WireMockServerResource>? NhsAuthMockService { get; private set; }

    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        applicationOptions.AssemblyName = typeof(Projects.External).Assembly.FullName;
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
            .Where(res => res.Name == "external-api" || res.Name == "matching-api" || res.Name == "yarp")
            .OfType<ProjectResource>()
            .ToArray();

        foreach (var resource in resources)
        {
            applicationBuilder.Resources.Remove(resource);
        }

        var externalApi = applicationBuilder.AddProject<Projects.External>("external-api")
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"] = $"{NhsAuthMockService?.GetEndpoint("http").Url}/personal-demographics/FHIR/R4/";
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"] = $"{NhsAuthMockService?.GetEndpoint("http").Url}/oauth2/token";
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES"] = "5";
            })
            .WaitFor(NhsAuthMockService!);

        var matchingApi = applicationBuilder.AddProject<Projects.Matching>("matching-api")
            .WithReference(externalApi)
            .WithSwaggerUi().WaitFor(NhsAuthMockService!);

        applicationBuilder.AddProject<Projects.Yarp>("yarp")
            .WithReference(matchingApi).WaitFor(matchingApi).WaitFor(NhsAuthMockService!);
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
}