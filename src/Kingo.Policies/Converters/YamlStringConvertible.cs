using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Kingo.Policies.Converters;

internal sealed class YamlStringConvertible<T>
    : IYamlTypeConverter
    where T : IStringConvertible<T>
{
    public bool Accepts(Type type) => type == typeof(T);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) =>
        parser.TryConsume<Scalar>(out var scalar)
            ? (object)(string.IsNullOrEmpty(scalar.Value) ? T.Empty() : T.From(scalar.Value))
            : throw new YamlException($"Expected a scalar value to deserialize to {typeof(T).Name}.");

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not T convertible)
            throw new ArgumentException($"Expected {typeof(T).Name} but got {value?.GetType()?.Name ?? "null"}");

        emitter.Emit(new Scalar(convertible?.ToString() ?? string.Empty));
    }
}
