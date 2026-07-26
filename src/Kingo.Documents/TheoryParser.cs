using Kingo.Theories;
using Results;
using System.Collections.Immutable;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Kingo.Documents;

public static class TheoryParser
{
    public static Result<Theory> Parse(string text) =>
        LoadDocument(text).Bind(ParseDocument);

    private static Result<Theory> ParseDocument(YamlMappingNode document) =>
        Result.Apply(
            ParseName(document).Map<Func<ImmutableArray<Namespace>, (TheoryName Name, ImmutableArray<Namespace> Namespaces)>>(
                domain => namespaces => (domain, namespaces)),
            ParseNamespaces(document))
            .Bind(domain => Theory.Create(domain.Name, domain.Namespaces));

    private static Result<TheoryName> ParseName(YamlMappingNode document) =>
        document.Children.TryGetValue(new YamlScalarNode(DocumentKeys.Name), out var name) && name is YamlScalarNode { Value: not null } scalar
            ? TheoryName.Parse(scalar.Value!)
            : Result.Failure<TheoryName>(Error.Validation(Diagnostics.ErrorCodes.Theory.Document, $"a theory document requires a '{DocumentKeys.Name}:' key naming the theory, with a scalar value"));

    private static Result<ImmutableArray<Namespace>> ParseNamespaces(YamlMappingNode document) =>
        document.Children.TryGetValue(new YamlScalarNode(DocumentKeys.Namespaces), out var namespaces) && namespaces is YamlMappingNode map
            ? map.Children.Select(ParseNamespace).Sequence()
            : Result.Failure<ImmutableArray<Namespace>>(Error.Validation(Diagnostics.ErrorCodes.Theory.Document, $"a theory document requires a '{DocumentKeys.Namespaces}:' key mapping namespace name to relation list"));

    private static Result<YamlMappingNode> LoadDocument(string text)
    {
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(text);
            stream.Load(reader);
            return stream.Documents is [{ RootNode: YamlMappingNode document }]
                ? Result.Success(document)
                : Result.Failure<YamlMappingNode>(Error.Validation(Diagnostics.ErrorCodes.Theory.Document, $"a theory document is a single YAML mapping carrying a '{DocumentKeys.Name}:' name and a '{DocumentKeys.Namespaces}:' map"));
        }
        catch (YamlException ex)
        {
            return Result.Failure<YamlMappingNode>(Error.Validation(Diagnostics.ErrorCodes.Theory.Syntax, $"malformed YAML: {ex.Message}"));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<YamlMappingNode>(Error.Validation(Diagnostics.ErrorCodes.Theory.Syntax, $"malformed YAML: {ex.Message}"));
        }
    }

    private static Result<Namespace> ParseNamespace(KeyValuePair<YamlNode, YamlNode> entry)
    {
        var name = entry.Key is YamlScalarNode key
            ? NamespaceName.Parse(key.Value!)
            : Result.Failure<NamespaceName>(Error.Validation(Diagnostics.ErrorCodes.Theory.Namespace, "a namespace name must be a scalar"));

        return Result.Apply(
            name.Map<Func<ImmutableArray<Relation>, (NamespaceName Name, ImmutableArray<Relation> Relations)>>(n => relations => (n, relations)),
            ParseRelations(entry.Value))
            .Bind(ns => Namespace.Create(ns.Name, ns.Relations));
    }

    private static Result<ImmutableArray<Relation>> ParseRelations(YamlNode node) =>
        node switch
        {
            YamlSequenceNode sequence => sequence.Children.Select(ParseRelation).Sequence(),
            YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" or "null" or "Null" or "NULL" or "~" } => Result.Success<ImmutableArray<Relation>>([]),
            _ => Result.Failure<ImmutableArray<Relation>>(Error.Validation(Diagnostics.ErrorCodes.Theory.Namespace, "a namespace defines a sequence of relations")),
        };

    private static Result<Relation> ParseRelation(YamlNode node) =>
        node switch
        {
            YamlScalarNode scalar => ParseRelationName(scalar.Value!).Map(relation => new Relation(relation)),
            YamlMappingNode { Children.Count: 1 } mapping => ParseRewriteRelation(mapping.Children.First()),
            _ => Result.Failure<Relation>(Error.Validation(Diagnostics.ErrorCodes.Theory.Relation, "a relation is a bare name or a single '<name>: <rewrite expression>' pair")),
        };

    private static Result<Relation> ParseRewriteRelation(KeyValuePair<YamlNode, YamlNode> entry) =>
        entry is { Key: YamlScalarNode name, Value: YamlScalarNode expression }
            ? Result.Apply(
                ParseRelationName(name.Value!)
                    .Map<Func<SubjectSetRewrite, Relation>>(relation => rewrite => new Relation(relation, rewrite)),
                ParseRewriteExpression(name, expression))
            : Result.Failure<Relation>(Error.Validation(Diagnostics.ErrorCodes.Theory.Relation, "a relation is a bare name or a single '<name>: <rewrite expression>' pair"));

    private static Result<SubjectSetRewrite> ParseRewriteExpression(YamlScalarNode name, YamlScalarNode expression) =>
        expression is { Style: ScalarStyle.Plain, Value: null or "" }
            ? Result.Failure<SubjectSetRewrite>(Error.Validation(Diagnostics.ErrorCodes.Theory.Relation, $"relation '{name.Value}' is missing its rewrite expression; a relation without a rewrite is a bare name"))
            : RewriteExpressionParser.Parse(expression.Value!);

    private static Result<RelationName> ParseRelationName(string name) =>
        RelationName.Parse(name).Bind(relation => RewriteExpressionPrinter.IsReserved(relation)
            ? Result.Failure<RelationName>(Error.Validation(Diagnostics.ErrorCodes.Theory.RelationReserved, $"'{relation}' is reserved by the rewrite grammar and cannot name a relation in a theory document"))
            : Result.Success(relation));
}
