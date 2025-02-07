using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ValidateApi.IntegrationTests;

public sealed class ValidateApiTests : IClassFixture<ValidateApiFixture>
{
	private readonly WebApplicationFactory<Program> _webApplicationFactory;
	private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public ValidateApiTests(ValidateApiFixture fixture)
	{
		_webApplicationFactory = fixture;
	}

	private HttpClient CreateHttpClient()
    {
        return _webApplicationFactory.CreateDefaultClient();
    }

    [Fact]
    public async Task Validate_ValidateData()
    {
		var _httpClient = CreateHttpClient();
        var response = await _httpClient.GetAsync("/api/v1/runvalidation");
        // Assert
        response.EnsureSuccessStatusCode();
        var validateResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("Data is Valid!!", validateResponse);
    }
}