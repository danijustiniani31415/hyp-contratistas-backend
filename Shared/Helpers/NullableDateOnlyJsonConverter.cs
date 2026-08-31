using System.Text.Json;
using System.Text.Json.Serialization;

namespace Abril_Backend.Shared.Helpers;

// El frontend envía "" (no null) cuando un <input type="date"> queda vacío.
// System.Text.Json no sabe convertir "" a DateOnly?, así que sin este converter
// el model binding falla antes de llegar al controller.
public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value)) return null;

        return DateOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        else writer.WriteNullValue();
    }
}
