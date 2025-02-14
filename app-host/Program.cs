using AppHost;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
	.WithLifetime(ContainerLifetime.Persistent)
	.WithHealthCheck();

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
    : builder.AddConnectionString("secrets");


var authApi = builder.AddProject<Projects.Auth>("auth-api")
					 .WithReference(redis).WaitFor(redis)
					 .WithReference(secrets)
					 .WithSwaggerUI();

var externalApi = builder.AddProject<Projects.External>("external-api")
					     .WithReference(authApi)
					     .WithReference(redis).WaitFor(redis)
					     .WithReference(secrets)
					     .WithSwaggerUI();

var matchingApi = builder.AddProject<Projects.Matching>("matching-api")
						 .WithReference(authApi)
						 .WithReference(externalApi)
						 .WithSwaggerUI();

builder.AddProject<Projects.Yarp>("yarp")
	   .WithReference(matchingApi).WaitFor(matchingApi);

builder.Build().Run();
