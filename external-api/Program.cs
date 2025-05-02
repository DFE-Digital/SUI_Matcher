using System.Diagnostics.CodeAnalysis;

using Shared.Endpoint;
using Shared.Exceptions;

using SUI.Core.Endpoints;
using SUI.Core.Endpoints.AuthToken;

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

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
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

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program;