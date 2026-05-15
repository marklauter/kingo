using Results;
using Values;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Kingo.Pdl.Converters;

internal sealed class YamlStringConvertible<T>
    : IYamlTypeConverter
    where T : IValue<T, string>
{
    public bool Accepts(Type type) => type == typeof(T);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (!parser.TryConsume<Scalar>(out var scalar))
            throw new YamlException($"Expected a scalar value to deserialize to {typeof(T).Name}.");

        return T.Parse(scalar.Value ?? string.Empty).Match<object>(
            onSuccess: value => value,
            onError: error => throw new YamlException($"Failed to parse {typeof(T).Name}: {error.Message}"));
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not T convertible)
            throw new ArgumentException($"Expected {typeof(T).Name} but got {value?.GetType()?.Name ?? "null"}");

        emitter.Emit(new Scalar(convertible.Value));
    }
}
