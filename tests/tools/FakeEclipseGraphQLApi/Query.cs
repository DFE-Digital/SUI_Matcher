using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using FakeEclipseGraphQLApi.Models;

namespace FakeEclipseGraphQLApi;

[ExcludeFromCodeCoverage]
public class Query
{
    public PersonResults GetPersonByCriteria(int? maxAge, RequestCursorInput? paging)
    {
        PersonResults? response = FetchPersonDataFromFile();

        if (response == null)
        {
            throw new InvalidOperationException("Failed to deserialize mock data from JSON file.");
        }

        if (response.Cursor != null && paging != null)
        {
            response.Cursor.PageNumber = paging.PageNumber ?? response.Cursor.PageNumber;
            response.Cursor.PageSize = paging.PageSize ?? response.Cursor.PageSize;
        }
        return response;

    }

    private static PersonResults? FetchPersonDataFromFile()
    {
        var filePath = System.IO.Path.Combine(AppContext.BaseDirectory, "data", "personByCriteria.json");
        if (!File.Exists(filePath))
        {
            filePath = System.IO.Path.Combine("data", "personByCriteria.json");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Mock data file not found at: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var response = JsonSerializer.Deserialize<PersonResults>(json, options);
        return response;
    }
}