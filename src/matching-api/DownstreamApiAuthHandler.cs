namespace MatchingApi;

using Microsoft.Identity.Web;
using System.Net.Http.Headers;

public class DownstreamApiAuthHandler : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string _downstreamApiScope;

    public DownstreamApiAuthHandler(ITokenAcquisition tokenAcquisition, IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        _downstreamApiScope = configuration.GetValue<string>("AzureAdMatching:Scopes");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string accessToken = await _tokenAcquisition.GetAccessTokenForAppAsync(
            _downstreamApiScope);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}