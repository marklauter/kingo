using Kingo.Theories;
using YamlDotNet.Serialization;

namespace Kingo.Documents;

/// <summary>
/// Renders a <see cref="Theory"/> to its theory document text, the serialize half of the theory document round trip (<see cref="TheoryParser.Parse"/> is the
/// other). An extension method, so the call site reads as a capability of the theory (<c>theory.Print()</c>) while the format knowledge stays in the
/// adapter.
/// </summary>
public static class TheoryPrinter
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n") // the document format owns its line ending, independent of platform
        .Build();

    /// <summary>
    /// Emits the theory document for <paramref name="theory"/>: the <c>theory:</c> name, then one namespace per key under <c>namespaces:</c> in theory
    /// order. Every name in the document is bare, and so is every name in the theory tree, so each key is written out as it is stored
    /// ([[identifiers]]). The theory's own invariants make the mapping well-formed by construction, because namespace names are unique.
    /// </summary>
    /// <returns>The theory document text for <paramref name="theory"/>.</returns>
    /// <exception cref="ArgumentException">A relation name or rewrite reference is the reserved word of the rewrite grammar (<c>this</c>), which cannot be expressed in a theory document.</exception>
    public static string Print(this Theory theory)
    {
        OrderedDictionary<string, List<object>> namespaces = new(theory.Namespaces.Length);
        foreach (var ns in theory.Namespaces)
            namespaces.Add(ns.Name.Value, [.. ns.Relations.Select(PrintRelation)]);

        return DocumentSerializer.Serialize(
            new OrderedDictionary<string, object>
            {
                [DocumentKeys.Name] = theory.Name.Value,
                [DocumentKeys.Namespaces] = namespaces,
            });
    }

    private static object PrintRelation(Relation relation) =>
        RewriteExpressionPrinter.IsReserved(relation.Name)
            ? throw new ArgumentException($"relation '{relation.Name}' cannot be expressed in a theory document: '{relation.Name}' is reserved by the rewrite grammar")
            : relation.Rewrite is SubjectSetRewrite.This
                ? relation.Name.Value
                : new Dictionary<string, string> { [relation.Name.Value] = RewriteExpressionPrinter.Print(relation.Rewrite) };
}
