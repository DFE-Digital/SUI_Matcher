using Aspire.Hosting.Testing;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ValidateApi.IntegrationTests;

public sealed class ValidateApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly IHost _app;

	public ValidateApiFixture()
	{
		var options = new DistributedApplicationOptions { AssemblyName = typeof(ValidateApiFixture).Assembly.FullName, DisableDashboard = true };
		var appBuilder = DistributedApplication.CreateBuilder(options);

		_app = appBuilder.Build();
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		builder.ConfigureWebHost(builder =>
		{
			builder.UseTestServer()
				.ConfigureServices(services =>
				{
					services.RemoveAll<IHostedService>();
				});

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