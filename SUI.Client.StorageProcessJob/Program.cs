// See https://aka.ms/new-console-template for more information

using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.StorageProcessJob;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<StorageProcessJobOptions>(
    builder.Configuration.GetSection(StorageProcessJobOptions.SectionName)
);

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

builder.Services.AddHttpClient();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var options = host.Services.GetRequiredService<IOptions<StorageProcessJobOptions>>();
logger.LogInformation("ACA Job started. Beginning CSV processing...");
logger.LogInformation("Storage process opt: {Val}", options.Value.MatchApiBaseAddress);
