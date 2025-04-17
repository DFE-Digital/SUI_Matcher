using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Client.Core;
using SUI.Client.Core.Extensions;

Console.Out.WriteAppName("SUI CSV File Processor");
Rule.Assert(args.Length == 1, "Usage: suic <csv-file>");
Rule.Assert(File.Exists(args[0]), $"File '{args[0]}' not found.");

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppSettingsJsonFile();
builder.ConfigureServices((hostContext, services) =>
{
    var matchingUrl = hostContext.Configuration["MatchApiBaseAddress"] ?? throw new Exception("Config item 'MatchApiBaseAddress' not set");
    services.AddClientCore(hostContext.Configuration, matchingUrl!);
});

var host = builder.Build();
var fileProcessor = host.Services.GetRequiredService<ICsvFileProcessor>();
var inputFile = args[0];
var outputDirectory = Path.GetDirectoryName(inputFile) ?? throw new Exception($"Directory name returned null for input: {inputFile}");
var outputFile = await fileProcessor.ProcessCsvFileAsync(inputFile, outputDirectory);

Console.WriteLine($"File processed; output={outputFile}");

await host.StopAsync();