using AppHost;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
    : builder.AddConnectionString("secrets");

var externalApi = builder.AddProject<Projects.External>("external-api")
					     .WithReference(secrets)
					     .WithSwaggerUI();

var matchingApi = builder.AddProject<Projects.Matching>("matching-api")
						 .WithReference(externalApi)
						 .WithSwaggerUI();

builder.AddProject<Projects.Yarp>("yarp")
	   .WithReference(matchingApi).WaitFor(matchingApi);

builder.Build().Run();
