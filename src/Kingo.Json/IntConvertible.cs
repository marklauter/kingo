using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kingo.Json;

public sealed class IntConvertible<T>
    : JsonConverter<T>
    where T : IIntConvertible<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
        ? reader.GetInt32() is int value ? T.From(value) : T.Empty()
        : throw new JsonException($"Expected a JSON string to deserialize to {typeof(T).Name}, but got {reader.TokenType}.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
