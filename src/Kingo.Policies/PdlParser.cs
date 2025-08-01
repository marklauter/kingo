using Kingo.Policies.Converters;
using LanguageExt;
using YamlDotNet.Serialization;

namespace Kingo.Policies;

public static class PdlParser
{
    public static Eff<PdlDocument> Parse(string yaml) =>
        Prelude.liftIO(() => Task.Run(() => ParsePdl(yaml)));

    private static PdlDocument ParsePdl(string yaml) =>
        new(yaml, Prelude.toSeq(
            new DeserializerBuilder()
            .WithTypeConverter(new RelationTypeConverter())
            .WithTypeConverter(new YamlStringConvertible<NamespaceIdentifier>())
            .WithTypeConverter(new YamlStringConvertible<RelationIdentifier>())
            .Build()
            .Deserialize<Dictionary<NamespaceIdentifier, List<Relation>>>(yaml)
            .Select(kvp =>
                new Namespace(kvp.Key, Prelude.toSeq(kvp.Value)))));
}
