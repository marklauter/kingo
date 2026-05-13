using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kingo.Json;

/// <summary>
/// JSON converter for any value type that implements <see cref="IStringConvertible{T}"/>. Serializes to and from a JSON string scalar.
/// </summary>
/// <typeparam name="T">The type to convert.</typeparam>
public sealed class StringConvertible<T>
    : JsonConverter<T>
    where T : IStringConvertible<T>
{
    /// <inheritdoc/>
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? reader.GetString() is string value ? T.From(value) : T.Empty()
            : throw new JsonException($"Expected JsonTokenType.String to deserialize to {typeof(T).Name}, but got {reader.TokenType}.");

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }

    /// <inheritdoc/>
    public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() is string value ? T.From(value) : T.Empty();

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WritePropertyName(value?.ToString() ?? string.Empty);
    }
}
