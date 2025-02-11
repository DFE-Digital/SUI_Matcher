using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/integration", () => 
{
    var json = File.ReadAllText("intConf.json");
    var jo = JsonSerializer.Deserialize<object>(json);
    return Results.Json(jo);
});

app.MapGet("/dev", () => 
{
    var json = File.ReadAllText("devConf.json");
    var jo = JsonSerializer.Deserialize<object>(json);
    return Results.Json(jo);
});

app.Run();