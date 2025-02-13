using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;
using WireMock.Client.Builders;

namespace AuthApi.IntegrationTests;

public sealed class NhsAuthMock(string baseUrl)
{
    private readonly IWireMockAdminApi _wireMockAdminApi =
        RestClient.For<IWireMockAdminApi>(new Uri(baseUrl, UriKind.Absolute));
    public static async Task SetupAsync(AdminApiMappingBuilder builder)
    {

        builder.Given(b => b
            .WithRequest(request => request
                .UsingPost()
                .WithPath("/oauth2/token")
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(() => new {
                        access_token = "12312321321321"
                }
            )
        ));

		await builder.BuildAndPostAsync();
	}
}