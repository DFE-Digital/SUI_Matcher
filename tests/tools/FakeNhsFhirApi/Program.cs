using FakeNhsFhirApi;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

const string baseDirectory = @"c:\temp\sui\fake-data";

var port = int.Parse(args.ElementAtOrDefault(0) ?? "8080");


using var server = WireMockServer.Start(new WireMock.Settings.WireMockServerSettings { Port = port, StartAdminInterface = true });

Console.WriteLine("WireMockServer running at {0}", string.Join(",", server.Ports));

var data = Generator.Generate(100);
Generator.WriteTestData(baseDirectory, data, port);
Console.WriteLine($"Written fake data to: {baseDirectory}");

static IResponseBuilder CreateResponse(string json) => Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(json);

// oauth
server
    .Given(Request.Create().UsingPost().WithPath("/oauth2/token"))
    .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
    {
        access_token = "12312321321321"
    }));


foreach (var item in data)
{
    server.Given(
            Request.Create()
                .UsingGet()
                .WithPath(Generator.PlaceholderSearchApiUri)
                .WithParam("given", true, item.Person.Given)
                .WithParam("family", true, item.Person.Family)
                .WithParam("birthdate", true, string.Concat("eq", item.Person.Dob))
        )
        .RespondWith(CreateResponse(item.ResponseJson));
}



Console.WriteLine("Press any key to stop the server");
Console.ReadKey();


server.Stop();