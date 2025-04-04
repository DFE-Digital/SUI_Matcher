using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Builders;
using Yarp.ReverseProxy.Configuration;

namespace ExternalApi.IntegrationTests;

public sealed class MockNhsFhirServer(string baseUrl)
{
    private readonly IWireMockAdminApi _wireMockAdminApi =
        RestClient.For<IWireMockAdminApi>(new Uri(baseUrl, UriKind.Absolute));
    public static async Task SetupAsync(AdminApiMappingBuilder builder)
    {

        builder.Given(b => b
            .WithRequest(request => request
                .UsingPost()
                .WithPath("/oauth2/token")
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(() => new {
                        access_token = "12312321321321"
                }
            )
        ));
        
        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient")
                .WithParams([
                    ParamMatch("given", "OCTAVIA"),
                    ParamMatch("family", "CHISLETT"),
                    ParamMatch("birthdate", "ge2008-09-21"),
                ])
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBody(() => File.ReadAllText("WireMockMappings/multi_match.json"))));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient")
                .WithParams([
                    ParamMatch("family", "CHISLETTE"),
                    ParamMatch("given", "OCTAVIAN")
                ])
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBody(() => File.ReadAllText("WireMockMappings/no_match.json"))));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient")
                .WithParams([
                    ParamMatch("_fuzzy-match", "True"),
                    ParamMatch("given", "OCTAVIA"),
                    ParamMatch("family", "CHISLETT"),
                    ParamMatch("birthdate", "eq2008-09-20"),
                ])
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBody(() => File.ReadAllText("WireMockMappings/single_match.json"))));
        
        builder.Given(b => b.WithRequest(r =>
        {
            r.UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient/9000000009");
        })
        .WithResponse(response => response.WithHeaders(h => h.Add("Content-Type", "application/json"))
            .WithBody(() => File.ReadAllText("WireMockMappings/single_patient_demographic.json"))));
        
        builder.Given(b => b.WithRequest(r =>
            {
                r.UsingGet()
                    .WithPath("/personal-demographics/FHIR/R4/Patient/9000000012");
            })
            .WithResponse(response => response.WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithStatusCode(400)));

		await builder.BuildAndPostAsync();
	}

    private static ParamModel ParamMatch(string name, string value)
    {
        return new ParamModel
        {
            Name = name,
            Matchers =
            [
                new MatcherModel
                {
                    Name = "ExactMatcher",
                    Pattern = value
                }
            ]
        };
    }
}