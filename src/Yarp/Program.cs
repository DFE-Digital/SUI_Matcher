using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;

using Shared.Aspire;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("FixedRateLimiter", opt =>
    {
        opt.PermitLimit = 60; // requests per time window
        opt.Window = TimeSpan.FromMinutes(1); // time window
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2000;
    });
});

builder.WebHost.UseKestrel(options =>
{
    options.AddServerHeader = false;
});

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
}

var app = builder.Build();

app.UseRateLimiter();

app.MapReverseProxy();

await app.RunAsync();