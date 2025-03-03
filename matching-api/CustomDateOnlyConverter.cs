using SUI.Core;
using System.Text.Json.Serialization;

namespace MatchingApi;

public class CustomDateOnlyConverter : JsonConverter<DateOnly?>
{
    private readonly string[] _formats = ["yyyyMMdd", "yyyy-MM-dd"];

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        else
        {
            return reader.GetString().ToDateOnly(_formats);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options) => writer.WriteStringValue(value?.ToString(_formats[0]));
}
