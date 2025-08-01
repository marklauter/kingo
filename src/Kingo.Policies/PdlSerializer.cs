using LanguageExt;
using YamlDotNet.Serialization;

namespace Kingo.Policies;

public static class PdlSerializer
{
    public static Eff<string> Serialize(PdlDocument document) =>
        Prelude.liftIO(() => Task.Run(() => SerializePdl(document)));

    private static string SerializePdl(PdlDocument document)
    {
        var namespaceDict = document.Namespaces.ToDictionary(
            ns => ns.Name.ToString(),  // Convert NamespaceIdentifier to string
            ns => ns.Relations.ToList());

        return new SerializerBuilder()
            .WithTypeConverter(new RelationTypeConverter())
            .Build()
            .Serialize(namespaceDict);
    }
}
