using Microsoft.AspNetCore.Routing;

namespace Shared.Endpoint;

public interface IEndpoint
{
	void MapEndpoint(IEndpointRouteBuilder app);
}
