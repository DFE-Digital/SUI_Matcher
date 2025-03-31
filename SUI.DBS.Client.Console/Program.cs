using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.DBS.Client.Core;
using SUI.DBS.Client.Core.Extensions;

Console.Out.WriteAppName("DBS Txt File Processor");
Rule.Assert(args.Length == 1, "Usage: dbsc <txt-file>");
Rule.Assert(File.Exists(args[0]), $"File '{args[0]}' not found.");

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    services.AddClientCore(hostContext.Configuration);
});

var host = builder.Build();
var fileProcessor = host.Services.GetRequiredService<ITxtFileProcessor>();
var inputFile = args[0];
var outputDirectory = Path.GetDirectoryName(inputFile) ?? throw new Exception($"Directory name returned null for input: {inputFile}");
await fileProcessor.ProcessFileAsync(inputFile);

Console.WriteLine($"File processed; for={inputFile}");

await host.StopAsync();