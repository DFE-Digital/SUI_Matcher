using System.Net;
using System.Text.Json.Nodes;

namespace ExternalApi.Util;

public class AuthClientCredentials
{
    private readonly HttpClient _client;
    private readonly string _tokenUrl;
    private readonly JwtHandler _jwtHandler;

    public AuthClientCredentials(string tokenUrl, string privateKey, string clientId, string kid, HttpClient? client = null)
    {
        _tokenUrl = tokenUrl;
        _client = client ?? new HttpClient();
        _jwtHandler = new JwtHandler(privateKey, tokenUrl, clientId, kid);
    }

    public async Task<string?> AccessToken(int expInMinutes = 1)
    {
        var jwt = _jwtHandler.GenerateJwt(expInMinutes);

        var values = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
            {"client_assertion", jwt},
        };
        var content = new FormUrlEncodedContent(values);

        Console.WriteLine("Requesting token from " + _tokenUrl);

        var response = await _client.PostAsync(_tokenUrl, content);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Authentication failed. \n" + response.Content);
        }

        var resBody = await response.Content.ReadAsStringAsync();
        var parsed = JsonNode.Parse(resBody);

        return parsed?["access_token"]?.ToString();

    }
}