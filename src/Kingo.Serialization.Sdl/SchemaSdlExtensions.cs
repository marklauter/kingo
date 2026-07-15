using Kingo.Schemas;
using YamlDotNet.Serialization;

namespace Kingo.Serialization.Sdl;

/// <summary>
/// Renders a <see cref="Schema"/> to its SDL document text — the serialize half of the SDL round trip (<see cref="SdlSerializer.Parse"/> is the other).
/// An extension so the call site reads as a domain capability (<c>schema.ToSdl()</c>) while the format knowledge stays in the adapter.
/// </summary>
public static class SchemaSdlExtensions
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n") // the document format owns its line ending, independent of platform
        .Build();

    /// <summary>
    /// Emits the SDL document for <paramref name="schema"/>, one namespace per mapping key in schema order. The schema's own invariants make the mapping
    /// well-formed by construction (namespace names are unique); the one document invariant the domain cannot express is the caller's defect and throws
    /// <see cref="ArgumentException"/>: no relationship name or rewrite reference may be a reserved word of the rewrite grammar (<c>this</c>, <c>...</c>).
    /// </summary>
    public static string ToSdl(this Schema schema)
    {
        OrderedDictionary<string, List<object>> document = new(schema.Namespaces.Length);
        foreach (var ns in schema.Namespaces)
            document.Add(ns.Name.Value, [.. ns.Relationships.Select(RenderRelationship)]);

        return DocumentSerializer.Serialize(document);
    }

    private static object RenderRelationship(Relationship relationship) =>
        RewriteExpressionRenderer.IsReserved(relationship.Name)
            ? throw new ArgumentException($"relationship '{relationship.Name}' cannot be expressed in SDL: '{relationship.Name}' is reserved by the rewrite grammar")
            : relationship.Rewrite is ThisRewrite
                ? relationship.Name.Value
                : new Dictionary<string, string> { [relationship.Name.Value] = RewriteExpressionRenderer.Render(relationship.Rewrite) };
}
