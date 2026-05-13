using Kingo.Pdl.Converters;
using System.Collections.Immutable;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Kingo.Pdl;

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
                .Select(kvp => new Namespace(kvp.Key, [.. kvp.Value ?? []]))
                .ToImmutableArray();

            return new PdlDocument(yaml, namespaces);
        }
        catch (YamlException ex)
        {
            throw new PdlParseException(PdlParseErrorCodes.SyntaxError, $"failed to parse PDL document: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            // Identifier validation in NamespaceIdentifier / RelationIdentifier throws ArgumentException for bad names.
            throw new PdlParseException(PdlParseErrorCodes.SyntaxError, $"failed to parse PDL document: {ex.Message}", ex);
        }
    }
}
