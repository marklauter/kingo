using Kingo.Schemas;
using YamlDotNet.Serialization;

namespace Kingo.Sdl;

/// <summary>
/// Prints a <see cref="Spec"/> to its SDL document text — the serialize half of the SDL round trip (<see cref="SpecParser.Parse"/> is the other).
/// An extension so the call site reads as a domain capability (<c>spec.Print()</c>) while the format knowledge stays in the adapter.
/// </summary>
public static class SpecPrinter
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n") // the document format owns its line ending, independent of platform
        .Build();

    /// <summary>
    /// Emits the SDL document for <paramref name="spec"/>: the <c>spec:</c> name, then one namespace per key under <c>namespaces:</c> in spec order.
    /// The spec's own invariants make the mapping well-formed by construction (namespace names are unique); the one document invariant the domain cannot
    /// express is the caller's defect and throws <see cref="ArgumentException"/>: no relationship name or rewrite reference may be the reserved word of the
    /// rewrite grammar (<c>this</c>).
    /// </summary>
    public static string Print(this Spec spec)
    {
        OrderedDictionary<string, List<object>> namespaces = new(spec.Namespaces.Length);
        foreach (var ns in spec.Namespaces)
            namespaces.Add(ns.Name.Value, [.. ns.Relationships.Select(PrintRelationship)]);

        return DocumentSerializer.Serialize(
            new OrderedDictionary<string, object>
            {
                ["spec"] = spec.Name.Value,
                ["namespaces"] = namespaces,
            });
    }

    private static object PrintRelationship(Relationship relationship) =>
        RewriteExpressionPrinter.IsReserved(relationship.Name)
            ? throw new ArgumentException($"relationship '{relationship.Name}' cannot be expressed in SDL: '{relationship.Name}' is reserved by the rewrite grammar")
            : relationship.Rewrite is ThisRewrite
                ? relationship.Name.Value
                : new Dictionary<string, string> { [relationship.Name.Value] = RewriteExpressionPrinter.Print(relationship.Rewrite) };
}
