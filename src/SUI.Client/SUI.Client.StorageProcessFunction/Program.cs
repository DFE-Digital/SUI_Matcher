using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Infrastructure.Http;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Application.Interfaces;
using SUI.StorageProcessFunction.Infrastructure.Azure;
using SUI.StorageProcessFunction.Infrastructure.Csv;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(
        (context, services) =>
        {
            services.Configure<StorageProcessFunctionOptions>(
                context.Configuration.GetSection(StorageProcessFunctionOptions.SectionName)
            );

            services.AddSingleton(TimeProvider.System);

            services.AddSingleton(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionString =
                    configuration["AzureWebJobsStorage"]
                    ?? throw new InvalidOperationException(
                        "AzureWebJobsStorage configuration is missing."
                    );

                return new BlobServiceClient(connectionString);
            });

            services.AddSingleton<IBlobStorageClient, AzureBlobStorageClient>();
            services.AddSingleton<IPersonSpecificationCsvParser, PersonSpecificationCsvParser>();
            services.AddHttpClient<IMatchingApiClient, MatchingApiClient>(
                (serviceProvider, client) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<IOptions<StorageProcessFunctionOptions>>()
                        .Value;

                    if (
                        string.IsNullOrWhiteSpace(options.MatchApiBaseAddress)
                        || !Uri.IsWellFormedUriString(options.MatchApiBaseAddress, UriKind.Absolute)
                    )
                    {
                        throw new InvalidOperationException(
                            "StorageProcessFunction MatchApiBaseAddress must be a valid absolute URI."
                        );
                    }

                    client.BaseAddress = new Uri(options.MatchApiBaseAddress);
                }
            );
            services.AddSingleton<
                IPersonSpecificationFileProcessor,
                PersonSpecificationFileOrchestrator
            >();
            services.AddSingleton<IStorageQueueMessageParser, EventGridStorageQueueMessageParser>();
            services.AddSingleton<IBlobFileOrchestrator, BlobFileOrchestrator>();
        }
    )
    .Build();

await host.RunAsync();
