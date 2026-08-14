using Azure.Core;
using Azure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Aspire;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Application.UseCases.ReconcilePeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.Core.Infrastructure.Http;
using SUI.Client.GraphQLProcessJob;
using SUI.Client.GraphQLProcessJob.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.AddTelemetryDefaults();

builder
    .Services.AddOptions<GraphQlProcessJobOptions>()
    .Bind(builder.Configuration.GetSection(GraphQlProcessJobOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
            options.ProcessingMode.Equals(
                ProcessingModes.Matching,
                StringComparison.OrdinalIgnoreCase
            )
            || options.ProcessingMode.Equals(
                ProcessingModes.Reconciliation,
                StringComparison.OrdinalIgnoreCase
            ),
        $"ProcessingMode must be {ProcessingModes.Matching} or {ProcessingModes.Reconciliation}."
    )
    .ValidateOnStart();

builder
    .Services.AddOptions<CsvMatchDataOptions>()
    .Bind(builder.Configuration.GetSection(CsvMatchDataOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<TokenCredential>(sp =>
{
    var options = sp.GetRequiredService<IOptions<GraphQlProcessJobOptions>>().Value;

    if (string.IsNullOrEmpty(options.TenantId) ||
        string.IsNullOrEmpty(options.ClientId) ||
        string.IsNullOrEmpty(options.ClientSecret))
    {
        throw new InvalidOperationException("Azure AD configuration is incomplete. TenantId, ClientId, and ClientSecret are required.");
    }

    return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
});

builder.Services.AddTransient<AzureAdAuthHandler>();

var eclipseClientBuilder = builder.Services.AddHttpClient("EclipseClient")
    .ConfigureHttpClient((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<GraphQlProcessJobOptions>>().Value;
        client.BaseAddress = new Uri(options.Url ?? throw new InvalidOperationException("The GraphQL Url must be set"));
    });

var useAuth = builder.Configuration.GetValue<bool>($"{GraphQlProcessJobOptions.SectionName}:UseAuth");
if (useAuth)
{
    eclipseClientBuilder.AddHttpMessageHandler<AzureAdAuthHandler>();
}

builder.Services.AddEclipseClient();

builder.Services.AddHttpClient<IMatchingApiClient, MatchingApiClient>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<GraphQlProcessJobOptions>>()
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

builder.Services.AddSingleton<MatchPersonRecordOrchestrator<CsvRecordDto>>();
builder.Services.AddSingleton<ReconcilePersonRecordOrchestrator<CsvRecordDto>>();
builder.Services.AddSingleton<IMatchPersonRecordOrchestrator<CsvRecordDto>>(serviceProvider =>
{
    var storageOptions = serviceProvider
        .GetRequiredService<IOptions<GraphQlProcessJobOptions>>()
        .Value;
    return storageOptions.ProcessingMode.Equals(
        ProcessingModes.Reconciliation,
        StringComparison.OrdinalIgnoreCase
    )
        ? serviceProvider.GetRequiredService<ReconcilePersonRecordOrchestrator<CsvRecordDto>>()
        : serviceProvider.GetRequiredService<MatchPersonRecordOrchestrator<CsvRecordDto>>();
});
builder.Services.AddSingleton<CsvPersonSpecParser>();
builder.Services.AddSingleton<IPersonSpecParser<CsvRecordDto>>(serviceProvider =>
    serviceProvider.GetRequiredService<CsvPersonSpecParser>()
);
builder.Services.AddSingleton<IReconciliationDataParser<CsvRecordDto>>(serviceProvider =>
    serviceProvider.GetRequiredService<CsvPersonSpecParser>()
);
builder.Services.AddSingleton<IPersonSpecParser<CsvRecordDto>, CsvPersonSpecParser>();
builder.Services.AddSingleton<ICsvHeadersProvider, CsvMatchingHeadersProvider>();
builder.Services.AddSingleton<TildePipeChronologicalAddressHistoryParser>();
builder.Services.AddSingleton<SemicolonCommaNewestFirstAddressHistoryParser>();
builder.Services.AddSingleton<ISourceAddressHistoryParser>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CsvMatchDataOptions>>().Value;
    return options.AddressHistoryFormat switch
    {
        SourceAddressHistoryFormat.TildePipeChronological =>
            serviceProvider.GetRequiredService<TildePipeChronologicalAddressHistoryParser>(),
        SourceAddressHistoryFormat.SemicolonCommaNewestFirst =>
            serviceProvider.GetRequiredService<SemicolonCommaNewestFirstAddressHistoryParser>(),
        _ => throw new InvalidOperationException(
            $"Unsupported address history format '{options.AddressHistoryFormat}'."
        ),
    };
});
builder.Services.AddSingleton<AddressComparisonOrchestrator>();
builder.Services.AddSingleton<GraphQlProcessor>();

using var host = builder.Build();
await host.StartAsync();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
logger.LogInformation("GraphQL job started");

try
{
    var processor = host.Services.GetRequiredService<GraphQlProcessor>();
    await processor.RunAsync(lifetime.ApplicationStopping);
}
catch (OperationCanceledException e) when (lifetime.ApplicationStopping.IsCancellationRequested)
{
    logger.LogInformation(e, "GraphQL job cancelled.");
}
catch (Exception e)
{
    logger.LogError(e, "GraphQL job failed.");
    Environment.ExitCode = 1;
}
finally
{
    await host.StopAsync();
}