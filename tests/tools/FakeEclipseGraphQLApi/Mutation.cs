using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using FakeEclipseGraphQLApi.Models;

namespace FakeEclipseGraphQLApi;

[ExcludeFromCodeCoverage]
public class Mutation
{
    public Person UpdatePerson(UpdatePerson input)
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
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        var response = JsonSerializer.Deserialize<PersonResults>(json, options);

        if (response == null || response.Results == null)
        {
            throw new InvalidOperationException("Failed to load and deserialize person data.");
        }

        var person = response.Results.FirstOrDefault(p => p.Id == input.Id);
        if (person == null)
        {
            throw new ArgumentException($"Person with ID '{input.Id}' not found.");
        }

        person.NhsNumber = input.NhsNumber;

        var updatedJson = JsonSerializer.Serialize(response, options);

        File.WriteAllText(filePath, updatedJson);

        return person;
    }
}