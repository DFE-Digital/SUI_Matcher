using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Shared.Extensions;

[ExcludeFromCodeCoverage(Justification = "Host configuration setup")]
public static class ResilienceHttpClientBuilderExtensions
{
    public static IHttpClientBuilder RemoveAllResilienceHandlers(this IHttpClientBuilder builder)
    {
        builder.ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
        {
#pragma warning disable EXTEXP0001
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i] is ResilienceHandler)
                {
                    handlers.RemoveAt(i);
                }
            }
#pragma warning restore EXTEXP0001
        });
        return builder;
    }
}