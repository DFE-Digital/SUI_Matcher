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

await builder.Build().RunAsync();
