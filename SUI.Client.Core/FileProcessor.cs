using CsvHelper;
using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using System.Globalization;

namespace SUI.Client.Core;

public interface IFileProcessor
{
    Task ProcessCsvFileAsync(string filePath);
}

public class FileProcessor(CsvMappingConfig mapping, IMatchPersonApiService matchPersonApi) : IFileProcessor
{
    private readonly CsvMappingConfig _mappingConfig = mapping ?? new();
    private readonly IMatchPersonApiService _matchPersonApi = matchPersonApi;

    public async Task ProcessCsvFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap(new DynamicCsvMap(_mappingConfig));

        await foreach (var record in csv.GetRecordsAsync<CsvRowModel>())
        {
            await ProcessLineAsync(record);
        }
    }

    private async Task ProcessLineAsync(CsvRowModel model)
    {
        var payload = new
        {
            // todo: map to personspecification
        };

        var result = await _matchPersonApi.MatchPersonAsync(payload);
        if(result != null)
        {


        }
    }

}
