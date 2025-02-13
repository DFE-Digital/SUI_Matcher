using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using AuthApi.Models;
using Asp.Versioning.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Xunit;

namespace AuthApi.IntegrationTests;

public sealed class AuthTest : IClassFixture<AuthApiFixture>
{
	private readonly WebApplicationFactory<Program> _webApplicationFactory;
	private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

	private readonly HttpClient _httpClient;
	private readonly string _redisConnectionString;

	public AuthTest(AuthApiFixture fixture)
	{
		_webApplicationFactory = fixture;
		_httpClient = _webApplicationFactory.CreateDefaultClient();
		_redisConnectionString = fixture.GetRedisConnectionString();
	}

	[Fact]
	public async Task Auth_Service_Returns_Token()
	{
		string token = "12312321321321";

		var redis = ConnectionMultiplexer.Connect(_redisConnectionString);
		var db = redis.GetDatabase();

		var response = await _httpClient.GetAsync($"/api/v1/get-token");
		response.EnsureSuccessStatusCode();

		var GetaccessTokenParts = await db.HashGetAllAsync(new RedisKey(NhsDigitalKeyConstants.AccountToken));
		var value = GetaccessTokenParts.ToList().ToDictionary(item => item.Name);

		var expTsString = value["absexp"].Value.ToString();
        var accessToken = value["data"].Value.ToString();

		var expirationDate = new DateTime(long.Parse(expTsString));
        var diffMinutes = (expirationDate - DateTime.Now).Minutes;

        Assert.True(diffMinutes is > 0 and <= NhsDigitalKeyConstants.AccountTokenRedisStorageExpiresInMinutes);    
        Assert.NotNull(expTsString);
		Assert.Equal(token, accessToken);
	}
}

