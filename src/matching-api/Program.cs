using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using MatchingApi;
using MatchingApi.Services;

using Microsoft.AspNetCore.Http.Json;

using Shared.Aspire;
using Shared.Endpoint;
using Shared.Exceptions;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IMatchingService, MatchingService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<INhsFhirClient, NhsFhirClientApiWrapper>();

builder.Services.AddHttpClient<INhsFhirClient, NhsFhirClientApiWrapper>(
    static client => client.BaseAddress = new("https+http://external-api"));

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

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program;