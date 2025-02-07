using Aspire.Hosting.Testing;
using AuthApi.Models;
using Shared.Helpers;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace sui_tests.Tests;

public class AuthEndpointIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task GetToken_UploadsToRedisWithExpirationDate()
    {
        // Arrange
        var appHost =
            await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var resourceNotificationService =
            app.Services.GetRequiredService<ResourceNotificationService>();
        await resourceNotificationService
            .WaitForResourceAsync("yarp", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(180));

        var httpClient = app.CreateHttpClient("yarp");

        var redis = appHost.Resources.ToList().Find(item => item.Name == "redis")!;
        if (redis is RedisResource res)
        {
            var database = await res.GetDatabase();
            
            // Assert it shouldn't exist yet 
            var accessToken = await database.StringGetWithExpiryAsync(NhsDigitalKeyConstants.AccountToken);
            Assert.False(accessToken.Expiry.HasValue);
        }
        
        // Act
        var response = await httpClient.GetAsync("matching/api/v1/matchperson");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        if (redis is RedisResource redisRes)
        {
            var database = await redisRes.GetDatabase();
            
            var accessTokenParts = await database.HashGetAllAsync(new RedisKey(NhsDigitalKeyConstants.AccountToken));
            
            var value = accessTokenParts.ToList().ToDictionary(item => item.Name);

            var expTsString = value["absexp"].Value.ToString();
            var accessToken = value["data"].Value.ToString();
            
            Assert.NotNull(expTsString);
            
            var expirationDate = new DateTime(long.Parse(expTsString));
            var diffMinutes = (expirationDate - DateTime.Now).Minutes;
            
            output.WriteLine($"expirationDate: {expirationDate}");
            output.WriteLine($"accessToken: {accessToken}");

            Assert.True(diffMinutes is > 0 and <= NhsDigitalKeyConstants.AccountTokenRedisStorageExpiresInMinutes);
            Assert.NotNull(accessToken);
        }
    }
}