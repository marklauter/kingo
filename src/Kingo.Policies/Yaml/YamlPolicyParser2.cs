using LanguageExt;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Kingo.Policies.Yaml;

/// <summary>
/// Parses a YAML string into a <see cref="PdlDocument"/>.
/// This is a secondary implementation that avoids direct use of LanguageExt in its primary logic,
/// relying on standard C# features and direct interaction with the YamlDotNet library.
/// </summary>
public static class YamlPolicyParser2
{
    /// <summary>
    /// Parses the specified YAML string into a PdlDocument.
    /// </summary>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <returns>A <see cref="PdlDocument"/> representing the parsed YAML.</returns>
    /// <exception cref="YamlException">Thrown if the YAML is invalid or the policy structure is incorrect.</exception>
    public static Eff<PdlDocument> Parse(string yaml) =>
        Prelude.liftIO(() => Task.Run(() => ParsePdl(yaml)));

    private static PdlDocument ParsePdl(string yaml)
    {
        var policySet = new NamespaceSet(Prelude.toSeq(
             new DeserializerBuilder()
             .WithTypeConverter(new RelationTypeConverter())
             .Build()
             .Deserialize<Dictionary<NamespaceIdentifier, List<Relation>>>(yaml)
             .Select(kvp =>
                 new Namespace(kvp.Key, Prelude.toSeq(kvp.Value)))));

        return new PdlDocument(yaml, policySet);
    }
}

/// <summary>
/// A custom type converter for deserializing a <see cref="Relation"/> from YAML.
/// This converter handles the polymorphic nature of a relation, which can be represented
/// as a simple string (e.g., "- owner") or a mapping (e.g., "- editor: this | owner").
/// </summary>
internal sealed class RelationTypeConverter
    : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Relation);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            // Handles the simple case: "- owner"
            return new Relation(RelationIdentifier.From(scalar.Value));
        }

        if (parser.TryConsume<MappingStart>(out _))
        {
            // Handles the complex case: "- editor: this | owner"
            var name = parser.Consume<Scalar>().Value;
            var expression = parser.Consume<Scalar>().Value;
            _ = parser.Consume<MappingEnd>();

            var rewriteResult = RewriteExpressionParser.Parse(expression).Run();

            return rewriteResult.Match(
                Succ: rewrite => new Relation(RelationIdentifier.From(name), rewrite),
                Fail: err => throw new YamlException($"Failed to parse rewrite expression '{expression}': {err.Message}")
            );
        }

        throw new YamlException("Unexpected token type for Relation deserialization. Expected a scalar or a mapping.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
        throw new NotImplementedException();
}
