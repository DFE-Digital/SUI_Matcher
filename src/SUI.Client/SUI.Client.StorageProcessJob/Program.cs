// See https://aka.ms/new-console-template for more information

using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.Core.Infrastructure.Http;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;
using SUI.Client.StorageProcessJob.Infrastructure;
using SUI.Client.StorageProcessJob.Infrastructure.Azure;

var builder = Host.CreateApplicationBuilder(args);

builder
    .Services.AddOptions<StorageProcessJobOptions>()
    .Bind(builder.Configuration.GetSection(StorageProcessJobOptions.SectionName))
    .Validate(
        options =>
            options.MessageVisibilityTimeoutMinutes > 0
            && options.MessageVisibilityRenewalIntervalMinutes > 0
            && options.MessageVisibilityRenewalIntervalMinutes
                < options.MessageVisibilityTimeoutMinutes,
        "Message visibility renewal interval must be positive and less than the visibility timeout."
    )
    .ValidateOnStart();

builder.Services.Configure<PersonMatchingOptions>(
    builder.Configuration.GetSection(PersonMatchingOptions.SectionName)
);

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString =
        configuration["AzureWebJobsStorage"]
        ?? throw new InvalidOperationException("AzureWebJobsStorage configuration is missing.");

    return new BlobServiceClient(connectionString);
});

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString =
        configuration["AzureWebJobsStorage"]
        ?? throw new InvalidOperationException("AzureWebJobsStorage configuration is missing.");

    return new QueueServiceClient(connectionString);
});

builder.Services.AddSingleton<IBlobStorageClient, AzureBlobStorageClient>();
builder.Services.AddSingleton<IStorageQueueClient, AzureStorageQueueClient>();
builder.Services.AddSingleton<IStorageQueueMessageParser, EventGridMessageParser>();
builder.Services.AddSingleton<IBlobFileOrchestrator, BlobFileOrchestrator>();
builder.Services.AddHttpClient<IMatchingApiClient, MatchingApiClient>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<StorageProcessJobOptions>>()
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

builder.Services.AddSingleton(
    typeof(IMatchPersonRecordOrchestrator<>),
    typeof(MatchPersonRecordOrchestrator<>)
);
builder.Services.AddSingleton<IPersonSpecParser<CsvRecordDto>>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<StorageProcessJobOptions>>().Value;
    return new CsvPersonSpecParser(options.CsvParserName);
});

builder.Services.AddHttpClient();

builder.Services.AddSingleton<QueueFileProcessor>();

using var host = builder.Build();
await host.StartAsync();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
logger.LogInformation("ACA Job started. Beginning CSV processing...");

try
{
    var processor = host.Services.GetRequiredService<QueueFileProcessor>();
    await processor.RunAsync(lifetime.ApplicationStopping);
}
catch (OperationCanceledException e) when (lifetime.ApplicationStopping.IsCancellationRequested)
{
    logger.LogInformation(e, "Storage process job cancelled.");
}
catch (Exception e)
{
    logger.LogError(e, "Storage process job failed.");
    throw;
}
finally
{
    await host.StopAsync();
}
