using Azure.Core;
using Azure.Identity;

using CommandLine;

using Shared.Util;

using SUI.Client.Core.Extensions;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Watcher;
using SUI.Client.Service.Watcher;

var watcherArgs= new WatcherArgs();
var argResult = await Parser.Default.ParseArguments<WatcherArgs>(args).WithParsedAsync(parsedArgs =>
{
    watcherArgs = parsedArgs;
    return Task.CompletedTask;
});
var builder = Host.CreateDefaultBuilder(args);
DotNetEnv.Env.TraversePath().Load();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration);
    services.AddWindowsService(options =>
    {
        options.ServiceName = "SUI-Client-Service";
    });
    services.AddSystemd();
    services.Configure<CsvWatcherConfig>(x =>
    {
        x.IncomingDirectory = argResult.Value.InputDirectory;
        x.ProcessedDirectory = argResult.Value.OutputDirectory;
        x.EnableGenderSearch = argResult.Value.EnableGenderSearch;
    });
    services.AddHttpClient<IMatchPersonApiService, MatchPersonApiService>(async void (client) =>
    {
        try
        {
            client.BaseAddress = new Uri(argResult.Value.ApiBaseUrl);
            if (hostContext.Configuration.GetValue<bool>("EnableAuth"))
            {
                var clientSecretCredential = new ClientSecretCredential(
                    hostContext.Configuration["AzureAdWatcher:TenantId"],
                    hostContext.Configuration["AzureAdWatcher:ClientId"],
                    hostContext.Configuration["AzureAdWatcher:ClientSecret"],
                    new ClientSecretCredentialOptions { AuthorityHost = new Uri(hostContext.Configuration["AzureAdWatcher:Authority"] ?? string.Empty) });
                var tokenRequestContext = new TokenRequestContext(
                    [hostContext.Configuration["AzureAdWatcher:Scopes"] ?? string.Empty]);
                AccessToken token = await clientSecretCredential.GetTokenAsync(tokenRequestContext);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
            }

            // Hack: until we can find a better way of routing for apps environment in azure.
            // Envoy uses SNI and needs a HOST header to route correctly
            // As we are using a private link, the host header needs to be set to the yarp hostname
            if (!argResult.Value.ApiBaseUrl.Contains("localhost"))
            {
                var uri = new Uri(argResult.Value.ApiBaseUrl);
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
    services.AddHostedService<Worker>();
});

var host = builder.Build();
host.Run();