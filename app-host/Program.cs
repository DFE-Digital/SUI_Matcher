using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
					// .WithContainerName("redis") // use an existing container
					.WithLifetime(ContainerLifetime.Persistent)
					.WithHealthCheck()
					.WithRedisCommander();

var matchingApi = builder.AddProject<Projects.Matching>("matching-api")
						 .WithSwaggerUI();

var validateApi = builder.AddProject<Projects.Validate>("validate-api")
						 .WithSwaggerUI();

builder.AddProject<Projects.Yarp>("yarp")
	   .WithReference(matchingApi).WaitFor(matchingApi)
	   .WithReference(validateApi).WaitFor(validateApi);

builder.Build().Run();
