using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Shared.Logging;

/// <summary>
/// Ref: https://andrewlock.net/customising-the-new-telemetry-logging-source-generator/
/// </summary>
/// <param name="httpContextAccessor"></param>
public class ApplicationEnricher(IHttpContextAccessor httpContextAccessor) : ILogEnricher
{
    public void Enrich(IEnrichmentTagCollector collector)
    {
        if (Activity.Current?.GetBaggageItem("SearchId") is { } searchId)
        {
            collector.Add("SearchId", searchId);
        }

        collector.Add("MachineName", Environment.MachineName);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            collector.Add("IsAuthenticated", httpContext?.User?.Identity?.IsAuthenticated!);
        }
    }
}