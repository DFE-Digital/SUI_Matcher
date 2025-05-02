using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ExternalApi.IntegrationTests;

public sealed class ExternalApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _app;
    public IResourceBuilder<WireMockServerResource> NhsAuthMockService { get; private set; }

    public ExternalApiFixture()
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = typeof(ExternalApiFixture).Assembly.FullName,
            DisableDashboard = true
        };
        var appBuilder = DistributedApplication.CreateBuilder(options);

        NhsAuthMockService = appBuilder.AddWireMock("mock-auth-api", WireMockServerArguments.DefaultPort)
            .WithApiMappingBuilder(MockNhsFhirServer.SetupAsync);

        _app = appBuilder.Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT", $"{NhsAuthMockService.GetEndpoint("http").Url}/personal-demographics/FHIR/R4/" },
                { "NhsAuthConfig:NHS_DIGITAL_TOKEN_URL", $"{NhsAuthMockService.GetEndpoint("http").Url}/oauth2/token" },
                { "NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES", "5" }
            }!);
        });

        return base.CreateHost(builder);
    }

    public async Task InitializeAsync()
    {
        await _app.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }
    }
}