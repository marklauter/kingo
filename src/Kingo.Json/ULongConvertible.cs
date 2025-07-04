using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kingo.Json;

public sealed class ULongConvertible<T>
    : JsonConverter<T>
    where T : IULongConvertible<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
        ? reader.GetUInt64() is ulong value ? T.From(value) : T.Empty()
        : throw new JsonException($"Expected a JSON string to deserialize to {typeof(T).Name}, but got {reader.TokenType}.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
