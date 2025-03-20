DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Environment.IsDevelopment())
{
    // Service discovery is not needed in Azure Container App Environment

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddServiceDiscoveryDestinationResolver();
}
else
{
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
    
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.ListenAnyIP(5443,
            portOptions => { portOptions.UseHttps(h => { h.UseLettuceEncrypt(kestrel.ApplicationServices); }); });
    });
    
    builder.Services.AddLettuceEncrypt();
}

var app = builder.Build();

app.MapReverseProxy();

app.Run();
