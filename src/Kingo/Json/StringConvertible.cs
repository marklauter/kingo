using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kingo.Json;

public sealed class StringConvertible<T>
    : JsonConverter<T>
    where T : IStringConvertible<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
        ? reader.GetString() is string value ? T.From(value) : T.Empty()
        : throw new JsonException($"Expected a JsonTokenType.String to deserialize to {typeof(T).Name}, but got {reader.TokenType}.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() is string value ? T.From(value) : T.Empty();

    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value?.ToString() ?? string.Empty);
}
