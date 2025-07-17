using AppHost.SwaggerUi;

using Aspire.Hosting.Testing;

using Azure.Core;
using Azure.Identity;

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

    public HttpClient CreateSecureClient()
    {
        var client = CreateHttpClient("external-api");
        var configuration = _app.Services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("EnableAuth"))
        {
            return client;
        }

        var clientSecretCredential = new ClientSecretCredential(
            configuration["AzureAdMatching:TenantId"],
            configuration["AzureAdMatching:ClientId"],
            configuration["AzureAdMatching:ClientSecret"],
            new ClientSecretCredentialOptions { AuthorityHost = new Uri(configuration["AzureAdMatching:Instance"]) });
        var tokenRequestContext = new TokenRequestContext(
            [configuration["AzureAdMatching:Scopes"]]);
        AccessToken token = clientSecretCredential.GetTokenAsync(tokenRequestContext).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");

        return client;
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