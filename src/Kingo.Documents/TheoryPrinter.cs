using Kingo.Theories;
using YamlDotNet.Serialization;

namespace Kingo.Documents;

public static class TheoryPrinter
{
    private static readonly ISerializer DocumentSerializer = new SerializerBuilder()
        .WithNewLine("\n")
        .Build();

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
