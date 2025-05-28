using AppHost.SwaggerUi;

DotNetEnv.Env.TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
    : builder.AddConnectionString("secrets");

var externalApi = builder.AddProject<Projects.External>("external-api")
                         .WithReference(secrets)
                         .WithSwaggerUi();

var matchingApi = builder.AddProject<Projects.Matching>("matching-api")
                         .WithReference(externalApi)
                         .WithSwaggerUi();

builder.AddProject<Projects.Yarp>("yarp")
    .WithReference(secrets)
    .WithReference(matchingApi).WaitFor(matchingApi);

await builder.Build().RunAsync();