using System.Net.Http.Headers;

using Azure.Core;

using Microsoft.Extensions.Options;

namespace SUI.Client.GraphQLProcessJob.Infrastructure;

public class AzureAdAuthHandler(IOptions<GraphQlProcessJobOptions> options, TokenCredential credential) : DelegatingHandler
{
    private readonly GraphQlProcessJobOptions _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var scope = _options.Scope ?? $"api://{_options.ClientId}/.default";
        var context = new TokenRequestContext([scope]);
        var tokenResult = await credential.GetTokenAsync(context, cancellationToken);

        if (!string.IsNullOrEmpty(tokenResult.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}