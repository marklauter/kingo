using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kingo.Json;

public class StringConvertible<T>
    : JsonConverter<T>
    where T : IStringConvertible<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && reader.GetString() is string value
        ? T.From(value)
        : throw new JsonException($"Expected a JSON string to deserialize to {typeof(T).Name}, but got {reader.TokenType}.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
