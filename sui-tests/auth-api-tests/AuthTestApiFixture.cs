using Aspire.Hosting.Testing;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AuthApi.IntegrationTests;

public sealed class AuthApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly IHost _app;
	public IResourceBuilder<RedisResource> Redis;
	private string  _redisConnectionString;
	public IResourceBuilder<WireMockServerResource> NhsAuthMockService { get; private set; }

	public AuthApiFixture()
	{
		var options = new DistributedApplicationOptions 
		{ 
			AssemblyName = typeof(AuthApiFixture).Assembly.FullName, 
			DisableDashboard = true 
		};
		var appBuilder = DistributedApplication.CreateBuilder(options);

		NhsAuthMockService = appBuilder.AddWireMock("nhsauthmock", WireMockServerArguments.DefaultPort)
		    .WithApiMappingBuilder(NhsAuthMock.SetupAsync);

		Redis = appBuilder.AddRedis("redis");

		_app = appBuilder.Build();
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"ConnectionStrings:{Redis.Resource.Name}", _redisConnectionString },
				{ "NhsAuthConfig:NHS_DIGITAL_TOKEN_URL", $"{NhsAuthMockService.GetEndpoint("http").Url}/oauth2/token" }
            }!);
        });

		return base.CreateHost(builder);
	}

	public async Task InitializeAsync()
    {
        await _app.StartAsync();

		_redisConnectionString = await Redis.Resource.GetConnectionStringAsync() ?? throw new InvalidOperationException("Redis connection string is null");
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

	public string GetRedisConnectionString()
    {
        return _redisConnectionString;
    }
}