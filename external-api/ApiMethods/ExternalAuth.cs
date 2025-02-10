using ExternalApi.Services;
using Shared.Endpoint;

namespace ExternalApi.ApiMethods;

public class ExternalAuthEndpoint(AuthServiceClient authServiceClient) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/perform-search", async () => 
		{
			await authServiceClient.GetToken();
			
			var value = "Person is Matched!!";
			return value;
		});
	}
}