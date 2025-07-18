namespace MatchingApi;

using System.Net.Http.Headers;

using Microsoft.Identity.Web;

public class DownstreamApiAuthHandler : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string _downstreamApiScope;

    public DownstreamApiAuthHandler(ITokenAcquisition tokenAcquisition, IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        _downstreamApiScope = configuration.GetValue<string>("AzureAdMatching:Scopes") ?? string.Empty;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string accessToken = await _tokenAcquisition.GetAccessTokenForAppAsync(
            _downstreamApiScope);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}