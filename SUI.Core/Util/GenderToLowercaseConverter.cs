using System.Text.Json;
using System.Text.Json.Serialization;

namespace SUI.Core.Util;

public class GenderToLowercaseConverter : JsonConverter<string?>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()?.ToLower() ?? string.Empty; // Convert input to lowercase and handle null
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToLower()); // Convert output to lowercase
    }
}