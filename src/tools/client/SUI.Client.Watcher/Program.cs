using Azure.Core;
using Azure.Identity;

using CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shared.Extensions;
using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;

Console.Out.WriteAppName("SUI CSV File Watcher");

var argResult = Parser.Default.ParseArguments<WatcherArgs>(args);

await argResult.WithParsedAsync(async parsedArgs =>
{
    var builder = Host.CreateDefaultBuilder();
    builder.ConfigureAppSettingsJsonFile();
    DotNetEnv.Env.TraversePath().Load();

    builder.ConfigureServices((hostContext, services) =>
    {
        services.AddClientCore(hostContext.Configuration);
        services.Configure<CsvWatcherConfig>(x =>
        {
            x.IncomingDirectory = parsedArgs.InputDirectory;
            x.ProcessedDirectory = parsedArgs.OutputDirectory;
            x.EnableGenderSearch = parsedArgs.EnableGenderSearch;
        });
        services.AddHttpClient<IMatchPersonApiService, MatchPersonApiService>(async void (client) =>
        {
            try
            {
                Rule.Assert(Uri.IsWellFormedUriString(parsedArgs.ApiBaseUrl, UriKind.Absolute),
                    "Invalid URL format for matching service URL.");
                client.BaseAddress = new Uri(parsedArgs.ApiBaseUrl);


                if (hostContext.Configuration.GetValue<bool>("EnableAuth"))
                {
                    var clientSecretCredential = new ClientSecretCredential(
                        hostContext.Configuration["AzureAdWatcher:TenantId"],
                        hostContext.Configuration["AzureAdWatcher:ClientId"],
                        hostContext.Configuration["AzureAdWatcher:ClientSecret"],
                        new ClientSecretCredentialOptions
                        {
                            AuthorityHost = new Uri(hostContext.Configuration["AzureAdWatcher:Authority"] ??
                                                    string.Empty)
                        });
                    var tokenRequestContext = new TokenRequestContext(
                        [hostContext.Configuration["AzureAdWatcher:Scopes"] ?? string.Empty]);
                    AccessToken token = await clientSecretCredential.GetTokenAsync(tokenRequestContext);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
                }

                // Hack: until we can find a better way of routing for apps environment in azure.
                // Envoy uses SNI and needs a HOST header to route correctly
                // As we are using a private link, the host header needs to be set to the yarp hostname
                if (!parsedArgs.ApiBaseUrl.Contains("localhost"))
                {
                    var uri = new Uri(parsedArgs.ApiBaseUrl);
                    var yarpHostValue = uri.Host.Replace(".privatelink.", ".").TrimEnd('/');
                    var hostUri = $"yarp.{yarpHostValue}";
                    client.DefaultRequestHeaders.Add("Host", hostUri);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    });

    var host = builder.Build();
    var cts = new CancellationTokenSource();
    var processor = host.Services.GetRequiredService<CsvFileMonitor>();
    var processingTask = processor.StartAsync(cts.Token);

    const string commandHelpMessage = "Type 'q' to quit, 'stats' for statistics.";
    Console.WriteLine($"File watcher started. {commandHelpMessage}");
    bool holdAppOpen = true;
    while (holdAppOpen)
    {
        var command = Console.ReadLine();
        switch (command?.ToLower())
        {
            case "q":
                await cts.CancelAsync();
                holdAppOpen = false;
                break;
            case "stats":
                processor.PrintStats(Console.Out);
                continue;
            default:
                Console.WriteLine($"Unknown command. {commandHelpMessage}");
                continue;
        }
    }

    await processingTask;

    await host.StopAsync();
    cts.Dispose();
});