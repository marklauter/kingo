using Kingo.Schemas;
using YamlDotNet.Serialization;

namespace Kingo.Sdl;

/// <summary>
/// Prints a <see cref="Schema"/> to its SDL document text — the serialize half of the SDL round trip (<see cref="SchemaParser.Parse"/> is the other).
/// An extension so the call site reads as a domain capability (<c>schema.Print()</c>) while the format knowledge stays in the adapter.
/// </summary>
public static class SchemaPrinter
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n") // the document format owns its line ending, independent of platform
        .Build();

    /// <summary>
    /// Emits the SDL document for <paramref name="schema"/>: the <c>schema:</c> name, then one namespace per key under <c>namespaces:</c> in schema order.
    /// The schema's own invariants make the mapping well-formed by construction (namespace names are unique); the one document invariant the domain cannot
    /// express is the caller's defect and throws <see cref="ArgumentException"/>: no relationship name or rewrite reference may be a reserved word of the
    /// rewrite grammar (<c>this</c>, <c>...</c>).
    /// </summary>
    public static string Print(this Schema schema)
    {
        OrderedDictionary<string, List<object>> namespaces = new(schema.Namespaces.Length);
        foreach (var ns in schema.Namespaces)
            namespaces.Add(ns.Name.Value, [.. ns.Relationships.Select(PrintRelationship)]);

        return DocumentSerializer.Serialize(
            new OrderedDictionary<string, object>
            {
                ["schema"] = schema.Name.Value,
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
