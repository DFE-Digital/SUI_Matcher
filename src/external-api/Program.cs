using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using ExternalApi;
using ExternalApi.Services;
using ExternalApi.Util;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

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


builder.Services.Configure<NhsAuthConfigOptions>(builder.Configuration.GetSection("NhsAuthConfig"));

// Setup client factory for external API calls
builder.Services.AddHttpClient("nhs-auth-api", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!);
    })
    .AddServiceDiscovery()
    .AddStandardResilienceHandler();

builder.Services.AddSingleton<SecretClient>(_ =>
{
    var keyVaultString = builder.Configuration.GetConnectionString("secrets") ?? throw new InvalidOperationException("Key Vault URI is not configured.");
    var uri = new Uri(keyVaultString);
    return new SecretClient(uri, new DefaultAzureCredential());
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

if (builder.Configuration.GetValue<bool>("EnableAuth"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(options =>
        {
            builder.Configuration.Bind("AzureAdExternal", options);
            options.TokenValidationParameters.NameClaimType = "name";

        }, options => { builder.Configuration.Bind("AzureAdExternal", options); });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("AuthPolicy", policy =>
            policy.RequireRole("ExternalApi"));
}

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
if (builder.Configuration.GetValue<bool>("EnableAuth"))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapDefaultEndpoints();
app.MapEndpoints(versionedGroup);
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
    await tokenService.Initialise();
}

await app.RunAsync();