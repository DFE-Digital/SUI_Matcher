var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(443, portOptions =>
    {
        portOptions.UseHttps(h =>
        {
            h.UseLettuceEncrypt(kestrel.ApplicationServices);
        });
    });
});

builder.AddServiceDefaults();

builder.Services.AddLettuceEncrypt();

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
}

var app = builder.Build();

app.MapReverseProxy();

app.Run();
