using System.Diagnostics.CodeAnalysis;

using ExternalApi.Services;
using ExternalApi.Util;

using Shared.Aspire;
using Shared.Endpoint;
using Shared.Exceptions;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ITokenService, StubTokenService>();
}
else
{
    builder.Services.AddSingleton<ITokenService, TokenService>();
}

builder.Services.AddSingleton<INhsFhirClient, NhsFhirClient>();
builder.Services.AddSingleton<IJwtHandler, JwtHandler>();
builder.Services.AddTransient<IFhirClientFactory, FhirClientFactory>();

// Setup client factory for external API calls
builder.Services.AddHttpClient("nhs-auth-api", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!);
    })
    .AddServiceDiscovery()
    .AddStandardResilienceHandler();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();

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

var app = builder.Build();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var versionedGroup = app
    .MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

app.UseExceptionHandler();

app.UseRouting();

app.MapDefaultEndpoints();
app.MapEndpoints(versionedGroup);
app.MapOpenApi();

await using (var scope = app.Services.CreateAsyncScope())
{
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
    await tokenService.Initialise();
}

await app.RunAsync();