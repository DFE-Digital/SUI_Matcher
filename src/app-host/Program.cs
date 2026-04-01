using Azure.Provisioning.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

DotNetEnv.Env.TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder
        .AddAzureKeyVault("secrets")
        .ConfigureInfrastructure(
            (infra) =>
            {
                var keyVault = infra.GetProvisionableResources().OfType<KeyVaultService>().Single();

                keyVault.Properties.Sku = new KeyVaultSku
                {
                    Family = KeyVaultSkuFamily.A,
                    Name = KeyVaultSkuName.Standard,
                };
                keyVault.Properties.EnableSoftDelete = true;
                keyVault.Properties.EnableRbacAuthorization = true;
                keyVault.Properties.EnablePurgeProtection = true;
            }
        )
    : builder.AddConnectionString("secrets");

var externalApi = builder
    .AddProject<Projects.External>("external-api")
    .WithReference(secrets)
    .WithUrlForEndpoint(
        "http",
        ep => new ResourceUrlAnnotation { Url = "/swagger", DisplayText = "Swagger UI" }
    );

var matchingApi = builder.AddProject<Projects.Matching>("matching-api");

// Feature flag setup
var auditLoggingFlag = builder.Configuration.GetValue<string>("FeatureToggles:EnableAuditLogging");
var auditLoggingEnabled = bool.Parse(auditLoggingFlag!);
var storageProcessFunctionFlag = builder.Configuration.GetValue<bool>(
    "FeatureToggles:EnableStorageProcessFunction"
);
matchingApi.WithEnvironment("FeatureManagement__EnableAuditLogging", auditLoggingFlag);

var storage =
    auditLoggingEnabled || storageProcessFunctionFlag
        ? builder.AddAzureStorage("sui-az-storage")
        : null;

if (storage is not null && builder.Environment.IsDevelopment())
{
    storage.RunAsEmulator(cfg =>
    {
        cfg.WithImageTag("3.35.0");
        cfg.WithBlobPort(10000);
        cfg.WithQueuePort(10001);
        cfg.WithTablePort(10002);
        cfg.WithLifetime(ContainerLifetime.Persistent);
    });
}

// Wrap in feature management to allow for feature toggling
if (auditLoggingEnabled)
{
    if (builder.Environment.IsDevelopment())
    {
        var table = storage!.AddTables("tables");
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
    .WithUrlForEndpoint(
        "http",
        ep => new ResourceUrlAnnotation { Url = "/swagger", DisplayText = "Swagger UI" }
    );

builder
    .AddProject<Projects.Yarp>("yarp")
    .WithExternalHttpEndpoints()
    .WithReference(matchingApi)
    .WaitFor(matchingApi);

if (storageProcessFunctionFlag)
{
    var incomingContainer = storage!.AddBlobContainer("storage-process-incoming", "incoming");
    var processedContainer = storage!.AddBlobContainer("storage-process-processed", "processed");
    var queue = storage!.AddQueue("storage-process-job-queue", "storage-process-job");
    var storageProcessFunction = builder
        .AddProject<Projects.SUI_Client_StorageProcessFunction>("storage-process-function")
        .WithReference(incomingContainer)
        .WaitFor(incomingContainer)
        .WithReference(processedContainer)
        .WaitFor(processedContainer)
        .WithReference(queue)
        .WaitFor(queue)
        .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
        .WithEnvironment("QueueName", "storage-process-job");

    if (builder.Environment.IsDevelopment())
    {
        storageProcessFunction.WithEnvironment("AzureWebJobsStorage", "UseDevelopmentStorage=true");
    }
    else
    {
        storageProcessFunction.WithEnvironment(
            "AzureWebJobsStorage",
            builder.AddConnectionString("AzureWebJobsStorage")
        );
    }
}

await builder.Build().RunAsync();
