using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MatchingApi.IntegrationTests;

public sealed class MatchingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly IHost _app;

	public MatchingApiFixture()
	{
		var options = new DistributedApplicationOptions { AssemblyName = typeof(MatchingApiFixture).Assembly.FullName, DisableDashboard = true };
		var appBuilder = DistributedApplication.CreateBuilder(options);

		_app = appBuilder.Build();
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		builder.ConfigureWebHost(b =>
		{
			b.UseTestServer()
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