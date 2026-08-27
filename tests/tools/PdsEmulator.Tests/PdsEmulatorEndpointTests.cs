using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PdsEmulator;

using Xunit;

namespace PdsEmulator.Tests;

public class PdsEmulatorEndpointTests
{
    private const string PatientSearchPath = "/personal-demographics/FHIR/R4/Patient";

    [Fact]
    public async Task Search_WithBirthDateRange_AppliesBothRepeatedValues()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await GetJson(
            client,
            $"{PatientSearchPath}?given=Jakub&family=Stedos&birthdate=ge2016-08-01&birthdate=le2016-09-01"
        );

        Assert.Equal(1, response.RootElement.GetProperty("total").GetInt32());
        Assert.Equal("9000000025", GetOnlyPatient(response).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Search_WithRepeatedGivenNames_RequiresEveryGivenName()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var store = factory.Services.GetRequiredService<DataStore>();
        store
            .Patients.Single(patient => patient.Id == "9000000025")
            .Name![0]
            .Given!
            .Add("Example");
        using var client = factory.CreateClient();

        using var response = await GetJson(
            client,
            $"{PatientSearchPath}?given=Jakub&given=Example&family=Stedos&birthdate=eq2016-08-30"
        );

        Assert.Equal(1, response.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Search_WithAllSupportedConstraints_ReturnsMatchingPatient()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await GetJson(
            client,
            $"{PatientSearchPath}?given=Jane&family=Smith&birthdate=eq2010-10-22"
                + "&address-postalcode=ls1*&gender=FEMALE"
                + "&email=jane.smith%40example.com&phone=01632960587"
        );

        Assert.Equal(1, response.RootElement.GetProperty("total").GetInt32());
        Assert.Equal("9000000009", GetOnlyPatient(response).GetProperty("id").GetString());
    }

    [Theory]
    [InlineData("address-postalcode=ZZ1*")]
    [InlineData("gender=male")]
    [InlineData("email=wrong%40example.com")]
    [InlineData("phone=00000000000")]
    public async Task Search_WithMismatchedConstraint_ReturnsNoPatients(string constraint)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await GetJson(
            client,
            $"{PatientSearchPath}?given=Jane&family=Smith&birthdate=eq2010-10-22&{constraint}"
        );

        Assert.Equal(0, response.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task SeededPatientIds_AreValidNhsNumbersAndPreserveEclipseIds()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var store = factory.Services.GetRequiredService<DataStore>();

        Assert.All(store.Patients, patient => Assert.True(IsValidNhsNumber(patient.Id), patient.Id));

        using var client = factory.CreateClient();
        using var response = await GetJson(client, $"{PatientSearchPath}/9000000025");
        var eclipseIdentifier = response
            .RootElement.GetProperty("identifier")
            .EnumerateArray()
            .Single(identifier =>
                identifier.GetProperty("system").GetString() == "urn:dfe:eclipse:person-id"
            );

        Assert.Equal("109908", eclipseIdentifier.GetProperty("value").GetString());
    }

    private static async Task<JsonDocument> GetJson(HttpClient client, string requestUri)
    {
        using var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
    }

    private static JsonElement GetOnlyPatient(JsonDocument response) =>
        response
            .RootElement.GetProperty("entry")
            .EnumerateArray()
            .Single()
            .GetProperty("resource");

    private static bool IsValidNhsNumber(string? value)
    {
        if (value is not { Length: 10 } || value.Any(character => !char.IsAsciiDigit(character)))
        {
            return false;
        }

        var weightedSum = Enumerable
            .Range(0, 9)
            .Sum(index => (value[index] - '0') * (10 - index));
        var expectedCheckDigit = 11 - (weightedSum % 11);
        expectedCheckDigit = expectedCheckDigit == 11 ? 0 : expectedCheckDigit;

        return expectedCheckDigit != 10 && expectedCheckDigit == value[9] - '0';
    }
}