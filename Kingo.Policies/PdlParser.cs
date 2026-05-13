using Kingo.Policies.Converters;
using System.Collections.Immutable;
using YamlDotNet.Serialization;

namespace Kingo.Policies;

public static class PdlParser
{
    public static PdlDocument Parse(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        try
        {
            var namespaces = new DeserializerBuilder()
                .WithTypeConverter(new RelationTypeConverter())
                .WithTypeConverter(new YamlStringConvertible<NamespaceIdentifier>())
                .WithTypeConverter(new YamlStringConvertible<RelationIdentifier>())
                .Build()
                .Deserialize<Dictionary<NamespaceIdentifier, List<Relation>>>(yaml)
                .Select(kvp => new Namespace(kvp.Key, [.. kvp.Value]))
                .ToImmutableArray();

            return new PdlDocument(yaml, namespaces);
        }
        catch (Exception ex) when (ex is not PdlParseException)
        {
            throw new PdlParseException(PdlParseErrorCodes.SyntaxError, $"failed to parse PDL document: {ex.Message}", ex);
        }
    }
}
