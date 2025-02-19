using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MatchingApi;
using SUI.Core.Domain;

namespace MatchingApi.IntegrationTests;

public sealed class MatchingTest : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _webApplicationFactory;
	private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

	public MatchingTest(WebApplicationFactory<Program> factory)
	{
		_webApplicationFactory = factory;
	}

	private HttpClient CreateHttpClient()
	{
		return _webApplicationFactory.CreateDefaultClient();
	}

    /*
	 
	>>> MIGRATED TO SUI.Core.Tests -> ValidateServiceTest.cs 
	 
	 */

    //[Theory]
    //[InlineData("", "Doe", "2000-01-01", "1234567890", "test@example.com", "male", "AB1 2CD", "Given name is required")]
    //[InlineData("John", "", "2000-01-01", "1234567890", "test@example.com", "male", "AB1 2CD", "Family name is required")]
    //[InlineData("John", "Doe", "2000-01-01", "invalid-phone", "test@example.com", "male", "AB1 2CD", "Invalid phone number.")]
    //[InlineData("John", "Doe", "2000-01-01", "1234567890", "invalid-email", "male", "AB1 2CD", "Invalid email address.")]
    //[InlineData("John", "Doe", "2000-01-01", "1234567890", "test@example.com", "invalid-gender", "AB1 2CD", "Gender has to match FHIR standards")]
    //[InlineData("John", "Doe", "2000-01-01", "1234567890", "test@example.com", "male", "invalid-postcode", "Invalid postcode.")]
    //public async Task Validate_InvalidData(string given, string family, string birthdate, string phone, string email, string gender, string addresspostalcode, string expectedErrorMessage)
    //{
    //	var httpClient = CreateHttpClient();

    //	var response = await httpClient.PostAsync("/api/v1/matchperson", JsonContent.Create(new PersonSpecification
    //	{
    //		Given = given,
    //		Family = family,
    //		BirthDate = Convert.ToDateTime(birthdate),
    //		Phone = phone,
    //		Email = email,
    //		Gender = gender,
    //		AddressPostalCode = addresspostalcode
    //	}));

    //	// Assert
    //	Assert.False(response.IsSuccessStatusCode);
    //       var validateResponse = await response.Content.ReadFromJsonAsync<ValidationResponse>(_jsonSerializerOptions);
    //       Assert.NotNull(validateResponse?.Results);
    //	Assert.Contains(validateResponse.Results, vr => vr.ErrorMessage == expectedErrorMessage);
    //}

    /*[Fact]
	public async Task Matching_SinglePerson()
	{
		// TODO
	}*/
}