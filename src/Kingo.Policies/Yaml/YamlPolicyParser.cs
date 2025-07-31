using YamlDotNet.Serialization;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Yaml;

public static class YamlPolicyParser
{
    public static PdlDocument? Parse(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlData = deserializer.Deserialize<Dictionary<string, List<object>>>(yaml);

            var policySet = TransformYamlToPolicySet(yamlData);
            return policySet != null ? new PdlDocument(yaml, policySet) : null;
        }
        catch
        {
            return null;
        }
    }

    private static NamespaceSet? TransformYamlToPolicySet(Dictionary<string, List<object>> yamlData)
    {
        var namespaces = new List<Namespace>();

        foreach (var (namespaceName, relations) in yamlData)
        {
            var relationList = new List<Relation>();

            foreach (var relationObj in relations)
            {
                switch (relationObj)
                {
                    case string relationName:
                        relationList.Add(new Relation(
                            RelationIdentifier.From(relationName),
                            DirectRewrite.Default));
                        break;

                    case Dictionary<object, object> relationDict:
                        var (name, expression) = relationDict.First();
                        var rewrite = RewriteExpressionParser.ParseRewriteExpression(expression.ToString()!);
                        if (rewrite == null)
                            return null;

                        relationList.Add(new Relation(
                            RelationIdentifier.From(name.ToString()!),
                            rewrite));
                        break;
                }
            }

            namespaces.Add(new Namespace(
                NamespaceIdentifier.From(namespaceName),
                toSeq(relationList)));
        }

        return new NamespaceSet(toSeq(namespaces));
    }
}
