using CsvHelper;
using SUI.Client.Core.Models;
using System.Globalization;
using System.Net.Http.Json;

namespace SUI.Client.Core;

public interface IFileProcessor
{
    Task ProcessCsvFileAsync(string filePath);
}

public class FileProcessor(CsvMappingConfig mapping, HttpClient httpClient) : IFileProcessor
{
    private readonly CsvMappingConfig _mappingConfig = mapping ?? new();
    private readonly HttpClient _httpClient = httpClient;

    public async Task ProcessCsvFileAsync(string filePath)
    {
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

        var response = await _httpClient.PostAsJsonAsync("api/endpoint", payload);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();

        if(dto != null)
        {


        }
    }

}
