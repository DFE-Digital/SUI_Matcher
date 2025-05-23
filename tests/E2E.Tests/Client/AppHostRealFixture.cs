using FluentAssertions.Primitives;

using Microsoft.Extensions.Hosting;

using WireMock.Admin.Requests;
using WireMock.Client;

namespace E2E.Tests.Client;

public sealed class AppHostRealFixture() : DistributedApplicationFactory(typeof(Projects.AppHost)), IAsyncLifetime
{
    private DistributedApplication? _app;

    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        applicationOptions.AssemblyName = typeof(Projects.AppHost).Assembly.FullName;
        applicationOptions.DisableDashboard = true;
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        
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
        
        DotNetEnv.Env.Load();

        var privateKey = Environment.GetEnvironmentVariable("NhsAuthConfig__NHS_DIGITAL_PRIVATE_KEY")!;
        var clientKey = Environment.GetEnvironmentVariable("NhsAuthConfig__NHS_DIGITAL_CLIENT_ID")!;

        var externalApi = applicationBuilder.AddProject<Projects.External>("external-api")
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_PRIVATE_KEY"] = privateKey;
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_CLIENT_ID"] = clientKey;
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"] =
                    "https://int.api.service.nhs.uk/personal-demographics/FHIR/R4/";
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"] =
                    "https://int.api.service.nhs.uk/oauth2/token";
                ctx.EnvironmentVariables["NhsAuthConfig:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES"] = "1";
            });

        var matchingApi = applicationBuilder.AddProject<Projects.Matching>("matching-api")
            .WithReference(externalApi)
            .WithSwaggerUI();

        applicationBuilder.AddProject<Projects.Yarp>("yarp")
            .WithReference(matchingApi).WaitFor(matchingApi);
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