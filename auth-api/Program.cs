using Shared.Endpoint;
using Shared.Exceptions;
using System.Diagnostics.CodeAnalysis;
using AuthApi.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddSingleton<ITokenService, StubTokenService>();
}
else
{
	builder.Services.AddSingleton<ITokenService, TokenService>();
}

builder.AddServiceDefaults();

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

builder.AddRedisDistributedCache(connectionName: "redis");

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
