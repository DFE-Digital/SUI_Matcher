using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Shared.Models;

using WireMock.Client;

namespace Integration.Tests.AppHost;

public class AppHostIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly HttpClient _client;
    private readonly IWireMockAdminApi _nhsAuthMockApi;
    private readonly AppHostFixture _fixture;

    private readonly JsonSerializerOptions _httpClientJsonOptions;

    public AppHostIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateSecureClient();
        _nhsAuthMockApi = fixture.NhsAuthMockApi();
        _httpClientJsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
        Assert.NotNull(personMatchResponse?.Result);
        Assert.NotNull(personMatchResponse.DataQuality);
        Assert.Equal(MatchStatus.Error, personMatchResponse.Result.MatchStatus);
        Assert.Equal(QualityType.NotProvided, personMatchResponse.DataQuality.Given);
        Assert.Equal(QualityType.NotProvided, personMatchResponse.DataQuality.Family);
        Assert.Equal(QualityType.Valid, personMatchResponse.DataQuality.BirthDate);

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
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
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

    [Fact]
    public async Task MatchingApi_Response_IncludesExcludes_Appropriate_Headers()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "OCTAVIA",
            Family = "CHISLETT",
            BirthDate = DateOnly.Parse("2008-09-20"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        Assert.False(response.Headers.Contains("Server"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));

    }

    // tests if the best in the bunch of ordered queries will be returned over the initial result

    [Fact]
    public async Task MatchingApi_BestOfTheBunchSearch()
    {
        var response = await _client.PostAsync("matching/api/v1/matchperson", JsonContent.Create(new PersonSpecification
        {
            Given = "Joe",
            Family = "Mock",
            BirthDate = DateOnly.Parse("2005-10-15"),
        }));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var personMatchResponse = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_httpClientJsonOptions);
        Assert.NotNull(personMatchResponse?.Result);
        Assert.Equal(MatchStatus.PotentialMatch, personMatchResponse.Result.MatchStatus);

        var calls = (await _nhsAuthMockApi.Should()).HaveReceived(4).Calls();
        calls.AtPathAndParamsWithResponse("/personal-demographics/FHIR/R4/Patient", new()
        {
            {"given", "Joe"},
            {"family", "Mock"},
            {"birthdate", "eq2005-10-15"},
        });
    }
}