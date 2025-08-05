using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

DotNetEnv.Env.TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
    : builder.AddConnectionString("secrets");

var externalApi = builder.AddProject<Projects.External>("external-api")
    .WithReference(secrets)
    .WithUrlForEndpoint("http", ep => new() { Url = "/swagger", DisplayText = "Swagger UI" });

var matchingApi = builder.AddProject<Projects.Matching>("matching-api");

// Feature flag setup
var auditLoggingFlag = builder.Configuration.GetValue<string>("FeatureToggles:EnableAuditLogging");
matchingApi.WithEnvironment("FeatureManagement__EnableAuditLogging", auditLoggingFlag);

// Wrap in feature management to allow for feature toggling
if (bool.Parse(auditLoggingFlag!))
{
    var storage = builder.AddAzureStorage("az-storage");

    if (builder.Environment.IsDevelopment())
    {
        storage.RunAsEmulator(cfg =>
        {
            cfg.WithImageTag("3.35.0");
            cfg.WithLifetime(ContainerLifetime.Persistent);
        });
        var table = storage.AddTables("tables");
        matchingApi.WithReference(table).WaitFor(table);
    }
    else
    {
        var table = builder.AddConnectionString("tables");
        matchingApi.WithReference(table).WaitFor(table);
    }
}

matchingApi
    .WithReference(externalApi)
    .WithHttpHealthCheck("health")
    .WithUrlForEndpoint("http", ep => new() { Url = "/swagger", DisplayText = "Swagger UI" });

builder.AddProject<Projects.Yarp>("yarp")
    .WithReference(secrets)
    .WithReference(matchingApi).WaitFor(matchingApi);

await builder.Build().RunAsync();