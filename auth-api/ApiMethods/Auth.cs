using AuthApi.Services;
using Shared.Endpoint;

namespace AuthApi.ApiMethods;

/**
 * https://digital.nhs.uk/developer/guides-and-documentation/tutorials/signed-jwt-authentication-c-tutorial
 */
public class AuthEndpoint(ITokenService tokenService) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		// Gets the access token from NHS Digital 
		app.MapGet("/get-token", async () => await tokenService.GetBearerToken());
	}

}