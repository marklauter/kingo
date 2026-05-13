using Kingo.Pdl.Converters;
using YamlDotNet.Serialization;

namespace Kingo.Pdl;

public static class PdlSerializer
{
    public static string Serialize(PdlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var namespaceDict = document.Namespaces.ToDictionary(
            ns => ns.Name,
            ns => ns.Relations.ToList());

        return new SerializerBuilder()
            .WithTypeConverter(new RelationTypeConverter())
            .WithTypeConverter(new YamlStringConvertible<NamespaceIdentifier>())
            .WithTypeConverter(new YamlStringConvertible<RelationIdentifier>())
            .Build()
            .Serialize(namespaceDict);
    }
}
