using Aspire.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace sui_tests.Tests
{
//     public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
//     {
//         private readonly IHost _app;
//         private EndpointReference _redisConnectionString;
//         private SecretClient? _stubKeyVaultClient;
//         private readonly EndpointReference _mockAzureKeyVaultServer;
//
//         /**
//          * Constructor for ApiFixture.
//          */
//         public ApiFixture()
//         {
//             var options = new DistributedApplicationOptions
//             {
//                 AssemblyName = typeof(ApiFixture).Assembly.FullName,
//                 DisableDashboard = true
//             };
//             var builder = DistributedApplication.CreateBuilder(options);
//             
//             /*var redis = builder.AddRedis("redis").PublishAsConnectionString();;
//
//             var secrets = builder.ExecutionContext.IsPublishMode
//                 ? builder.AddAzureKeyVault("secrets")
//                 : builder.AddConnectionString("secrets");
//
//             var authApi = builder.AddProject<Projects.Auth>("auth-api")
//                 .WithReference(redis).WaitFor(redis)
//                 .WithReference(secrets);*/
//             
//             _app = builder.Build();
//         }
//
//         /**
//          * Creates and configures the host for the application.
//          *
//          * @param builder The IHostBuilder instance.
//          * @return The configured IHost instance.
//          */
//         protected override IHost CreateHost(IHostBuilder builder)
//         {
//             builder.ConfigureHostConfiguration(config =>
//             {
//                 config.AddInMemoryCollection(new Dictionary<string, string?>
//                 {
//                     { "Redis:Configuration", "localhost:6379" },
//                     { "KeyVault:Uri", "http://localhost:5200/" },
//                 }!);
//             });
//             
//             builder.ConfigureServices((context, services) =>
//             {
//                 /*services.AddStackExchangeRedisCache(options =>
//                 {
//                     options.Configuration = context.Configuration["Redis:Configuration"];
//                 });*/
//
//                 // Add stub Key Vault client for testing
//                 _stubKeyVaultClient = new SecretClient(new Uri(context.Configuration["KeyVault:Uri"]!), new DefaultAzureCredential());
//                 services.AddSingleton(_stubKeyVaultClient);
//             });
//
//             var host = base.CreateHost(builder);
//             return host;
//         }
//
//         /**
//          * Disposes the resces used by the fixture asynchronously.
//          * Stops the application host and disposes of it.
//          */
//         public new async Task DisposeAsync()
//         {
//             await base.DisposeAsync();
//             await _app.StopAsync();
//             //_redis?.Dispose();
//             if (_app is IAsyncDisposable asyncDisposable)
//             {
//                 await asyncDisposable.DisposeAsync().ConfigureAwait(false);
//             }
//             else
//             {
//                 _app.Dispose();
//             }
//         }
//
//         /**
//          * Initializes the fixture asynchronously.
//          */
//         public async Task InitializeAsync()
//         {
//             await _app.StartAsync();
//
//             // Initialize Redis connection
//             // TODO: replace with environment variable
//             //_redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
//             
//         }
//     }
}
