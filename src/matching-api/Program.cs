using System.Text.Json.Serialization;

using Asp.Versioning.Builder;

using DotNetEnv;

using MatchingApi;
using MatchingApi.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Identity.Web;

using Shared.Aspire;
using Shared.Endpoint;
using Shared.Exceptions;

Env.TraversePath().Load();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

if (builder.Configuration.GetValue<bool>("EnableAuth"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(options =>
        {
            builder.Configuration.Bind("AzureAdMatching", options);
            options.TokenValidationParameters.NameClaimType = "name";
        }, options => { builder.Configuration.Bind("AzureAdMatching", options); })
        .EnableTokenAcquisitionToCallDownstreamApi(options => { })
        .AddInMemoryTokenCaches();

    builder.Services.AddAuthorization(config =>
    {
        config.AddPolicy("AuthPolicy", policy =>
            policy.RequireRole("MatchingApi"));
    });
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IMatchingService, MatchingService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<INhsFhirClient, NhsFhirClientApiWrapper>();

IHttpClientBuilder client =
    builder.Services.AddHttpClient<INhsFhirClient, NhsFhirClientApiWrapper>(static client =>
        client.BaseAddress = new Uri("https+http://external-api"));

if (builder.Configuration.GetValue<bool>("EnableAuth"))
{
    builder.Services.AddTransient<DownstreamApiAuthHandler>();
    client.AddHttpMessageHandler<DownstreamApiAuthHandler>();
}

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpoints(typeof(Program).Assembly);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new CustomDateOnlyConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

WebApplication app = builder.Build();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

RouteGroupBuilder versionedGroup = app
    .MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

app.UseExceptionHandler();

app.UseRouting();
if (builder.Configuration.GetValue<bool>("EnableAuth"))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapDefaultEndpoints();
app.MapEndpoints(versionedGroup);
app.MapOpenApi();

await app.RunAsync();