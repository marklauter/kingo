using Kingo.Domains;
using YamlDotNet.Serialization;

namespace Kingo.Sdl;

/// <summary>
/// Renders a <see cref="Domain"/> to its SDL document text, the serialize half of the SDL round trip (<see cref="DomainParser.Parse"/> is the
/// other). An extension method, so the call site reads as a domain capability (<c>domain.Print()</c>) while the format knowledge stays in the
/// adapter.
/// </summary>
public static class DomainPrinter
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n") // the document format owns its line ending, independent of platform
        .Build();

    /// <summary>
    /// Emits the SDL document for <paramref name="domain"/>: the <c>domain:</c> name, then one namespace per key under <c>namespaces:</c> in spec
    /// order. Every name in the document is bare, and so is every name in the spec tree, so each key is written out as it is stored
    /// ([[identifiers]]). The spec's own invariants make the mapping well-formed by construction, because namespace names are unique.
    /// </summary>
    /// <returns>The SDL document text for <paramref name="domain"/>.</returns>
    /// <exception cref="ArgumentException">A relationship name or rewrite reference is the reserved word of the rewrite grammar (<c>this</c>), which cannot be expressed in SDL.</exception>
    public static string Print(this Domain domain)
    {
        OrderedDictionary<string, List<object>> namespaces = new(domain.Namespaces.Length);
        foreach (var ns in domain.Namespaces)
            namespaces.Add(ns.Name.Value, [.. ns.Relationships.Select(PrintRelationship)]);

        return DocumentSerializer.Serialize(
            new OrderedDictionary<string, object>
            {
                ["domain"] = domain.Name.Value,
                ["namespaces"] = namespaces,
            });
    }

    private static object PrintRelationship(Relationship relationship) =>
        RewriteExpressionPrinter.IsReserved(relationship.Name)
            ? throw new ArgumentException($"relationship '{relationship.Name}' cannot be expressed in SDL: '{relationship.Name}' is reserved by the rewrite grammar")
            : relationship.Rewrite is SubjectSetRewrite.This
                ? relationship.Name.Value
                : new Dictionary<string, string> { [relationship.Name.Value] = RewriteExpressionPrinter.Print(relationship.Rewrite) };
}
