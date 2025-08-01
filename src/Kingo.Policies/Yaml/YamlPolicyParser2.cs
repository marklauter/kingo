using LanguageExt;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Kingo.Policies.Yaml;

public static class YamlPolicyParser2
{
    public static Eff<PdlDocument> Parse(string yaml) =>
        Prelude.liftIO(() => Task.Run(() => ParsePdl(yaml)));

    private static PdlDocument ParsePdl(string yaml) =>
        new(yaml, Prelude.toSeq(
             new DeserializerBuilder()
             .WithTypeConverter(new RelationTypeConverter())
             .Build()
             .Deserialize<Dictionary<NamespaceIdentifier, List<Relation>>>(yaml)
             .Select(kvp =>
                 new Namespace(kvp.Key, Prelude.toSeq(kvp.Value)))));
}

sealed file class RelationTypeConverter
    : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Relation);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        // Handles the simple case: "- owner"
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            return new Relation(RelationIdentifier.From(scalar.Value));
        }

        // Handles the complex case: "- editor: this | owner"
        if (parser.TryConsume<MappingStart>(out _))
        {
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
