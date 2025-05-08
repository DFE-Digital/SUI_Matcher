using RestEase;

using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Builders;

namespace Integration.Tests.AppHost;

public sealed class MockNhsFhirServer(string baseUrl)
{
    private readonly IWireMockAdminApi _wireMockAdminApi =
        RestClient.For<IWireMockAdminApi>(new Uri(baseUrl, UriKind.Absolute));
    public static async Task SetupAsync(AdminApiMappingBuilder builder)
    {
        static string GetFileText(string fileName) => File.ReadAllText(Path.Combine("WireMockMappings", fileName));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingPost()
                .WithPath("/oauth2/token")
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(() => new
                {
                    access_token = "12312321321321"
                }
            )
        ));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient")
                .WithParams([
                    ParamMatch("given", "John"),
                    ParamMatch("family", "Doe"),
                    ParamMatch("birthdate", "eq2010-01-01"),
                ])
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBody(() => GetFileText("multi_match.json"))));

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
                .WithBody(() => GetFileText("no_match.json"))));

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
                .WithBody(() => GetFileText("single_match.json"))));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient")
                .WithParams([
                    ParamMatch("_fuzzy-match", "True"),
                    ParamMatch("given", "Hannah"),
                    ParamMatch("family", "Robinson"),
                    ParamMatch("birthdate", "eq2005-10-15"),
                ])
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBody(() => GetFileText("single_match_low_confidence.json"))));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/personal-demographics/FHIR/R4/Patient")
                .WithParams([
                    ParamMatch("_fuzzy-match", "True"),
                    ParamMatch("given", "Joe"),
                    ParamMatch("family", "Robinson"),
                    ParamMatch("birthdate", "eq2005-10-15"),
                ])
            )
            .WithResponse(response => response
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBody(() => GetFileText("single_match_really_low_confidence.json"))));

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