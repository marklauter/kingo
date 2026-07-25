using Kingo.Theories;
using YamlDotNet.Serialization;

namespace Kingo.Documents;

/// <summary>
/// Renders a <see cref="Theory"/> to its domain document text, the serialize half of the domain document round trip (<see cref="TheoryParser.Parse"/> is the
/// other). An extension method, so the call site reads as a domain capability (<c>domain.Print()</c>) while the format knowledge stays in the
/// adapter.
/// </summary>
public static class TheoryPrinter
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n") // the document format owns its line ending, independent of platform
        .Build();

    /// <summary>
    /// Emits the domain document for <paramref name="domain"/>: the <c>domain:</c> name, then one namespace per key under <c>namespaces:</c> in domain
    /// order. Every name in the document is bare, and so is every name in the domain tree, so each key is written out as it is stored
    /// ([[identifiers]]). The domain's own invariants make the mapping well-formed by construction, because namespace names are unique.
    /// </summary>
    /// <returns>The domain document text for <paramref name="domain"/>.</returns>
    /// <exception cref="ArgumentException">A relation name or rewrite reference is the reserved word of the rewrite grammar (<c>this</c>), which cannot be expressed in a domain document.</exception>
    public static string Print(this Theory domain)
    {
        OrderedDictionary<string, List<object>> namespaces = new(domain.Namespaces.Length);
        foreach (var ns in domain.Namespaces)
            namespaces.Add(ns.Name.Value, [.. ns.Relations.Select(PrintRelation)]);

        return DocumentSerializer.Serialize(
            new OrderedDictionary<string, object>
            {
                [DocumentKeys.Name] = domain.Name.Value,
                [DocumentKeys.Namespaces] = namespaces,
            });
    }

    private static object PrintRelation(Relation relation) =>
        RewriteExpressionPrinter.IsReserved(relation.Name)
            ? throw new ArgumentException($"relation '{relation.Name}' cannot be expressed in a domain document: '{relation.Name}' is reserved by the rewrite grammar")
            : relation.Rewrite is SubjectSetRewrite.This
                ? relation.Name.Value
                : new Dictionary<string, string> { [relation.Name.Value] = RewriteExpressionPrinter.Print(relation.Rewrite) };
}
