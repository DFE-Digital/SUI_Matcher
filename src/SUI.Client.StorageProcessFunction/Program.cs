using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;
using SUI.StorageProcessFunction.Infrastructure.AzureStorage;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(
        (context, services) =>
        {
            services.Configure<StorageProcessFunctionOptions>(
                context.Configuration.GetSection(StorageProcessFunctionOptions.SectionName)
            );

            services.AddSingleton(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionString = configuration["AzureWebJobsStorage"]
                    ?? throw new InvalidOperationException(
                        "AzureWebJobsStorage configuration is missing."
                    );

                return new BlobServiceClient(connectionString);
            });

            services.AddSingleton<IBlobFileReader, AzureBlobFileReader>();
            services.AddSingleton<IBlobPayloadProcessor, PlaceholderBlobPayloadProcessor>();
            services.AddSingleton<StorageQueueMessageProcessor>();
        }
    )
    .Build();

await host.RunAsync();
