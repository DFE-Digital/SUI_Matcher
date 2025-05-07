using System.Net.Http.Json;

using Shared.Models;

using SUI.Core.Domain;

using WireMock.Client;

namespace AppHost.IntegrationTests;

public class AppHostIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly HttpClient _client;
    private readonly IWireMockAdminApi _nhsAuthMockApi;
    private readonly AppHostFixture _fixture;

    public AppHostIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateHttpClient("yarp");
        _nhsAuthMockApi = fixture.NhsAuthMockApi();
    }

    public static IEnumerable<object[]> GetEndpoints()
    {
        yield return ["matching-api", "/health"];
        yield return ["external-api", "/health"];
    }

    [Fact]
    public async Task AppHostRunsCleanly()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(GetEndpoints))]
    public async Task AppHostApiChecks(string endpointName, string endpointUrl)
    {
        using var httpClient = _fixture.CreateHttpClient(endpointName);
        var response = await httpClient.GetAsync(endpointUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Single Match with high confidence score (>95%, Confirmed match)

    [Fact]
    public async Task MatchingApi_Match()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "OCTAVIA",
            Family = "CHISLETT",
            BirthDate = DateOnly.Parse("2008-09-20"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.Match, personMatchResponse.Result.MatchStatus);
    }

    // Single Match with low confidence (<95%, Candidate Match)

    [Fact]
    public async Task MatchingApi_SingleMatchWithLowConfidence()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "Hannah",
            Family = "Robinson",
            BirthDate = DateOnly.Parse("2005-10-15"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.PotentialMatch, personMatchResponse.Result.MatchStatus);
    }

    // Single Match with really low confidence (<85%, Candidate Match)

    [Fact]
    public async Task MatchingApi_SingleMatchWithReallyLowConfidence()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "Joe",
            Family = "Robinson",
            BirthDate = DateOnly.Parse("2005-10-15"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.NoMatch, personMatchResponse.Result.MatchStatus);
    }

    [Fact]
    public async Task MatchingApi_NoMatch()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "OCTAVIAN",
            Family = "CHISLETTE",
            BirthDate = DateOnly.Parse("2008-09-21"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.NoMatch, personMatchResponse.Result.MatchStatus);
    }

    // Multiple Matches

    [Fact]
    public async Task MatchingApi_ManyMatch()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("2010-01-01"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.ManyMatch, personMatchResponse.Result.MatchStatus);
    }

    // No match with additional conditions

    // Client supplies incorrect data and gets error

    [Fact]
    public async Task MatchingApi_InvalidSearchData_ErrorResponse()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "",
            BirthDate = DateOnly.Parse("2010-01-01"),
        }));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.NotNull(personMatchResponse.DataQuality);
        Assert.Equal(MatchStatus.Error, personMatchResponse.Result.MatchStatus);
        Assert.Equal(QualityType.NotProvided, personMatchResponse.DataQuality.Given);
        Assert.Equal(QualityType.NotProvided, personMatchResponse.DataQuality.Family);
        Assert.Equal(QualityType.Valid, personMatchResponse.DataQuality.Birthdate);

    }

    // Client supplies enough data to get a match, but some invalid fields

    [Fact]
    public async Task MatchingApi_MatchWithSomeInvalidFields()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "OCTAVIA",
            Family = "CHISLETT",
            BirthDate = DateOnly.Parse("2008-09-20"),
            Email = "not an email",
            Gender = "car",
            Phone = "hello",
            AddressPostalCode = "12@24"
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.Match, personMatchResponse.Result.MatchStatus);
        Assert.NotNull(personMatchResponse.DataQuality);
        Assert.Equal(QualityType.Invalid, personMatchResponse.DataQuality.Email);
        Assert.Equal(QualityType.Invalid, personMatchResponse.DataQuality.Gender);
        Assert.Equal(QualityType.Invalid, personMatchResponse.DataQuality.Phone);
        Assert.Equal(QualityType.Invalid, personMatchResponse.DataQuality.AddressPostalCode);
    }

    // JWT renewal (needed for technical conformance.)

    [Fact(Skip = "Explicit run only due to Task.Delay")]
    public async Task MatchingApi_ExpireTheAccessToken_TokenRenews()
    {
        for (var i = 0; i < 2; i++)
        {
            await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
            {
                Given = "OCTAVIA",
                Family = "CHISLETT",
                BirthDate = DateOnly.Parse("2008-09-20"),
            }));
        }

        // Confirms that the access token is cached
        (await _nhsAuthMockApi.Should()).HaveReceived(1).Calls()
            .AtPath("/oauth2/token");

        await Task.Delay(TimeSpan.FromMinutes(1));

        await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "OCTAVIA",
            Family = "CHISLETT",
            BirthDate = DateOnly.Parse("2008-09-20"),
        }));

        // Confirms that a new token was requested
        (await _nhsAuthMockApi.Should()).HaveReceived(2).Calls()
            .AtPath("/oauth2/token");
    }
}