using Microsoft.AspNetCore.Mvc;

using PdsEmulator;

var builder = WebApplication.CreateBuilder(args);

// Add context/datastore
builder.Services.AddSingleton(new DataStore("data.json"));

var app = builder.Build();

app.MapPost("/oauth2/token", () => Results.Ok(new { access_token = "12312321321321" }));

app.MapGet("/personal-demographics/FHIR/R4/Patient", (HttpRequest request, [FromServices] DataStore store) =>
{
    var givenRaw = request.Query["given"].ToString();
    var familyRaw = request.Query["family"].ToString();
    var birthdateRaw = request.Query["birthdate"].ToString();

    var query = store.Patients.AsEnumerable();

    if (!string.IsNullOrEmpty(givenRaw))
    {
        var given = givenRaw.ToLowerInvariant();
        query = query.Where(p => p.Name != null && p.Name.Any(n => n.Given != null && n.Given.Any(g => g.ToLowerInvariant() == given)));
    }

    if (!string.IsNullOrEmpty(familyRaw))
    {
        var family = familyRaw.ToLowerInvariant();
        query = query.Where(p => p.Name != null && p.Name.Any(n => n.Family != null && n.Family.ToLowerInvariant() == family));
    }

    if (!string.IsNullOrEmpty(birthdateRaw))
    {
        string op = birthdateRaw.Length > 2 ? birthdateRaw.Substring(0, 2) : "eq";
        string dateStr = birthdateRaw.StartsWith(op) ? birthdateRaw.Substring(2) : birthdateRaw;

        if (DateTime.TryParse(dateStr, out var dateValue))
        {
            query = query.Where(p =>
            {
                if (DateTime.TryParse(p.BirthDate, out var pDate))
                {
                    return op switch
                    {
                        "eq" => pDate == dateValue,
                        "ge" => pDate >= dateValue,
                        "le" => pDate <= dateValue,
                        "gt" => pDate > dateValue,
                        "lt" => pDate < dateValue,
                        _ => pDate == dateValue,
                    };
                }
                return false;
            });
        }
        else
        {
            // exact match string fallback
            query = query.Where(p => p.BirthDate == birthdateRaw);
        }
    }

    var results = query.ToList();

    if (results.Count > 1)
    {
        return Results.Ok(new
        {
            resourceType = "OperationOutcome",
            issue = new[]
            {
                new {
                    code = "multiple-matches",
                    severity = "information",
                    details = new {
                        coding = new[] {
                            new { code = "TOO_MANY_MATCHES", display = "Too Many Matches", system = "https://fhir.nhs.uk/R4/CodeSystem/Spine-ErrorOrWarningCode", version = "1" }
                        }
                    }
                }
            }
        });
    }

    var entries = results.Select(p => new
    {
        fullUrl = $"https://int.api.service.nhs.uk/personal-demographics/FHIR/R4/Patient/{p.Id}",
        resource = p,
        search = new { score = 1 }
    }).ToArray();

    return Results.Ok(new
    {
        resourceType = "Bundle",
        type = "searchset",
        timestamp = DateTime.UtcNow.ToString("O"),
        total = entries.Length,
        entry = entries
    });
});

app.MapGet("/personal-demographics/FHIR/R4/Patient/{id}", (string id, [FromServices] DataStore store) =>
{
    if (id == "9000000012") return Results.BadRequest(); // explicit error test case support

    var patient = store.Patients.FirstOrDefault(p => p.Id == id);
    if (patient == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(patient);
});

// Health check endpoint for container probes
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();