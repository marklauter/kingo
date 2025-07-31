using LanguageExt;
using YamlDotNet.Serialization;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Yaml;

public static class YamlPolicyParser
{
    public static Eff<PdlDocument> Parse(string yaml)
    {
        var policySet = TransformYamlToPolicySet(Deserialize(yaml));
        return policySet != null
            ? Pure(new PdlDocument(yaml, policySet))
            : ParseError.New(ParseErrorCodes.ParseEerror, "Failed to parse YAML");
    }

    private static Dictionary<string, List<object>> Deserialize(string yaml) =>
        new DeserializerBuilder()
        .Build()
        .Deserialize<Dictionary<string, List<object>>>(yaml);

    private static NamespaceSet TransformYamlToPolicySet(Dictionary<string, List<object>> yamlData)
    {
        var namespaces = yamlData
            .Select(kvp => TransformNamespace(kvp.Key, kvp.Value))
            .Where(ns => ns is not null)
            .Select(ns => ns!);

        return new NamespaceSet(Seq(namespaces));
    }

    private static Namespace? TransformNamespace(string namespaceName, List<object> relations)
    {
        var relationResults = relations
            .Select(TransformRelation)
            .ToArray();

        return relationResults.Any(r => r == null)
            ? null
            : new Namespace(
                NamespaceIdentifier.From(namespaceName),
                toSeq(relationResults.Where(r => r is not null).Select(r => r!)));
    }

    private static Relation? TransformRelation(object relationObj) =>
        relationObj switch
        {
            string relationName => CreateSimpleRelation(relationName),
            Dictionary<object, object> relationDict => CreateComplexRelation(relationDict),
            _ => throw new NotSupportedException("unexpected relation")
        };

    private static Relation CreateSimpleRelation(string relationName) =>
        new(RelationIdentifier.From(relationName), DirectRewrite.Default);

    private static Relation? CreateComplexRelation(Dictionary<object, object> relationDict)
    {
        var (name, expression) = relationDict.First();
        var rewriteResult = RewriteExpressionParser.Parse(expression.ToString()!).Run();

        return rewriteResult.IsSucc
            ? new Relation(
                RelationIdentifier.From(name.ToString()!),
                rewriteResult.ThrowIfFail())
            : null;
    }
}
