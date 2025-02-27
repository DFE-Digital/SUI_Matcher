using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.Client.Core;
using SUI.Client.Core.Integration;

const string Name = "********** SUI CSV File Processor **********";
var p = new string('*', Name.Length);

Console.WriteLine(p); 
Console.WriteLine(Name);
Console.WriteLine(p);
Console.WriteLine();

if (args.Length == 0)
{
    Console.WriteLine("Usage: suic <csv-file>");
}
else
{
    var csvFilePath = args[0];

    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    var apiBaseAddress = config["MatchApiBaseAddress"] ?? throw new Exception("Config item 'MatchApiBaseAddress' not set");
    var mapping = config.GetSection("CsvMapping").Get<CsvMappingConfig>() ?? new CsvMappingConfig();

    var services = new ServiceCollection()
        .AddSingleton(mapping)
        .AddSingleton(x => new HttpClient() { BaseAddress = new Uri(apiBaseAddress) })
        .AddSingleton<IMatchPersonApiService, MatchPersonApiService>()
        .AddSingleton<IFileProcessor, FileProcessor>()
        .BuildServiceProvider();

    await services.GetRequiredService<IFileProcessor>().ProcessCsvFileAsync(csvFilePath, Path.GetDirectoryName(csvFilePath));
    
    Console.WriteLine("File processed.");
}