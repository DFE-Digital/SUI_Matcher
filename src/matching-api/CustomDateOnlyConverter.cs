using System.Text.Json.Serialization;

using Shared.Util;

namespace MatchingApi;

public class CustomDateOnlyConverter : JsonConverter<DateOnly?>
{
    private readonly string[] _formats = [Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish];

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.ToDateOnly(_formats);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options) => writer.WriteStringValue(value?.ToString(_formats[0]));
}