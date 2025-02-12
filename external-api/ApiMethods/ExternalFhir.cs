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
		app.MapPost("/search", async (SearchQuery query) =>
		{
			var validationResults = new List<ValidationResult>();
			var context = new ValidationContext(query);
			bool isValid = Validator.TryValidateObject(query, context, validationResults, true);

			if (!isValid)
			{
				//return Results.BadRequest(validationResults);
			}
			
			return await fhirClient.PerformSearch(query);
		});
	}
}