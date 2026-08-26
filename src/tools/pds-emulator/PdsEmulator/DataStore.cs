using System.Text.Json;

namespace PdsEmulator;

public class DataStore
{
    public List<Patient> Patients { get; } = new();

    public DataStore(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<Patient>>(json, options);
            if (list != null)
            {
                Patients = list;
            }
        }
    }
}