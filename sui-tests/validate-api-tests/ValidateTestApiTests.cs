using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using ValidateApi.Models;
using Xunit;

namespace ValidateApi.IntegrationTests;

public sealed class ValidateTest : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _webApplicationFactory;
	private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

	public ValidateTest(WebApplicationFactory<Program> factory)
	{
		_webApplicationFactory = factory;
	}

	private HttpClient CreateHttpClient()
	{
		return _webApplicationFactory.CreateDefaultClient();
	}

	[Theory]
	[InlineData("", "Doe", "2000-01-01", "1234567890", "test@example.com", "male", "AB1 2CD", "Given name is required")]
	[InlineData("John", "", "2000-01-01", "1234567890", "test@example.com", "male", "AB1 2CD", "Family name is required")]
	[InlineData("John", "Doe", "invalid-date", "1234567890", "test@example.com", "male", "AB1 2CD", "Incorrect Date Format")]
	[InlineData("John", "Doe", "2000-01-01", "invalid-phone", "test@example.com", "male", "AB1 2CD", "Invalid phone number.")]
	[InlineData("John", "Doe", "2000-01-01", "1234567890", "invalid-email", "male", "AB1 2CD", "Invalid email address.")]
	[InlineData("John", "Doe", "2000-01-01", "1234567890", "test@example.com", "invalid-gender", "AB1 2CD", "Gender has to match FHIR standards")]
	[InlineData("John", "Doe", "2000-01-01", "1234567890", "test@example.com", "male", "invalid-postcode", "Invalid postcode.")]
	public async Task Validate_InvalidData(string given, string family, string birthdate, string phone, string email, string gender, string addresspostalcode, string expectedErrorMessage)
	{
		var _httpClient = CreateHttpClient();
		var response = await _httpClient.GetAsync($"/api/v1/runvalidation?given={given}&family={family}&birthdate={birthdate}&phone={phone}&email={email}&gender={gender}&addresspostalcode={addresspostalcode}");
		
		// Assert
		Assert.False(response.IsSuccessStatusCode);
        var validateResponse = await response.Content.ReadFromJsonAsync<ValidationResponse>(_jsonSerializerOptions);
		Assert.Contains(validateResponse.ValidationResults, vr => vr.ErrorMessage == expectedErrorMessage);
	}

	private class ValidationResponse
	{
		public IEnumerable<ValidationResult> ValidationResults { get; set; }
	}

	private class ValidationResult
	{
		public IEnumerable<string> MemberNames { get; set; }
		public string ErrorMessage { get; set; }
	}
}