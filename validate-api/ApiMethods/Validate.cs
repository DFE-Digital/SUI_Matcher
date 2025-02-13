using Microsoft.AspNetCore.Mvc;
using ValidateApi.Models;
using Shared.Endpoint;
using Shared.OpenTelemetry;
using System.ComponentModel.DataAnnotations;

namespace ValidateApi.ApiMethods;

public class ValidateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/runvalidation", (string given, string family, string birthdate, string? phone, string? email, string? gender, string? addresspostalcode) =>
        {
			
			if (!DateTime.TryParse(birthdate, out DateTime dob))
		    {
			    // Not a valid date, so return error.
				var birthresponse = new
				{
                    ValidationResults = new[]
                    {
                        new
                        {
                            MemberNames = new string[] { "BirthDate" },
                            ErrorMessage = "Incorrect Date Format"
                        }
                    }
				};
			    return Results.BadRequest(birthresponse);
		    }
            var personSpecification = new PersonSpecification
            {
                Given = given,
                Family = family,
                BirthDate = DateTime.Parse(birthdate),
                Gender = gender,
                Phone = phone,
                Email = email,
                AddressPostalCode = addresspostalcode
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(personSpecification, null, null);
            bool isValid = Validator.TryValidateObject(personSpecification, validationContext, validationResults, true);

            var response = new
            {
                ValidationResults = validationResults.Select(vr => new
                {
                    vr.MemberNames,
                    vr.ErrorMessage
                })
            };

            if (!isValid)
            {
                return Results.BadRequest(response);
            }

            return Results.Ok(response);
        });
    }
}