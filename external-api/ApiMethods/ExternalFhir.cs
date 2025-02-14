using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using ExternalApi.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Shared.Endpoint;
using Shared.Models;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace ExternalApi.ApiMethods;

public class ExternalFhirEndpoint(NhsFhirClient fhirClient) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/search", async (SearchQuery query) => await fhirClient.PerformSearch(query));
	}
}