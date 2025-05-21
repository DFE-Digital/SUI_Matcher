
using System.Diagnostics.CodeAnalysis;

using HealthChecks.Uris;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppHost;

/// <summary>
/// Ref: https://github.com/davidfowl/WaitForDependenciesAspire/tree/main/WaitForDependencies.Aspire.Hosting
/// </summary>
[ExcludeFromCodeCoverage]
public static class Extensions
{
    public static IResourceBuilder<T> WithHealthCheck<T>(
        this IResourceBuilder<T> builder,
        string? endpointName = null,
        string path = "health",
        Action<UriHealthCheckOptions>? configure = null)
        where T : IResourceWithEndpoints
    {
        return builder.WithAnnotation(new HealthCheckAnnotation((resource, ct) =>
        {
            if (resource is not IResourceWithEndpoints resourceWithEndpoints)
            {
                return Task.FromResult<IHealthCheck?>(null);
            }

            var endpoint = endpointName is null
             ? resourceWithEndpoints.GetEndpoints().FirstOrDefault(e => e.Scheme is "http" or "https")
             : resourceWithEndpoints.GetEndpoint(endpointName);

            var url = endpoint?.Url;

            if (url is null)
            {
                return Task.FromResult<IHealthCheck?>(null);
            }

            var options = new UriHealthCheckOptions();

            options.AddUri(new Uri(new Uri(url), path));

            configure?.Invoke(options);

            var client = new HttpClient();
            return Task.FromResult<IHealthCheck?>(new UriHealthCheck(options, () => client));
        }));
    }
}

[ExcludeFromCodeCoverage]
public class HealthCheckAnnotation(Func<IResource, CancellationToken, Task<IHealthCheck?>> healthCheckFactory) : IResourceAnnotation
{
    public Func<IResource, CancellationToken, Task<IHealthCheck?>> HealthCheckFactory { get; } = healthCheckFactory;

    public static HealthCheckAnnotation Create(Func<string, IHealthCheck> connectionStringFactory)
    {
        return new HealthCheckAnnotation(async (resource, token) =>
        {
            if (resource is not IResourceWithConnectionString c)
            {
                return null;
            }

            if (await c.GetConnectionStringAsync(token) is not { } cs)
            {
                return null;
            }

            return connectionStringFactory(cs);
        });
    }
}