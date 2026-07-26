using Kingo.Theories;
using Results;
using System.Collections.Immutable;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Kingo.Documents;

/// <summary>
/// The parse half of the theory document adapter: theory document text to the core domain model
/// (<see cref="TheoryPrinter.Print"/> renders the other direction). YAML carries the theory name and the outer namespace map. Each relation's
/// optional rewrite expression is an embedded mini-language handled by <see cref="RewriteExpressionParser"/> and
/// <see cref="RewriteExpressionPrinter"/>. Parsing exits through the core's validating factories (<c>RelationName.Parse</c>,
/// <c>NamespaceName.Parse</c>, <c>TheoryName.Parse</c>, <c>Namespace.Create</c>, <c>Theory.Create</c>), accumulating every document-level,
/// identifier-level, and expression-level error into one <see cref="Result{T}"/> failure.
/// </summary>
public static class TheoryParser
{
    /// <summary>Parses untrusted theory document text into the defined <see cref="Theory"/>.</summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the defined <see cref="Theory"/>, or every accumulated validation <see cref="Error"/> in
    /// document order. <c>theory.syntax</c> for malformed YAML. <c>theory.document</c> when the text is not a single mapping, or the <c>theory:</c>
    /// or <c>namespaces:</c> keys are missing or misshapen. <c>theory.namespace</c> or <c>theory.relation</c> for wrong node shapes, or a
    /// <c>&lt;name&gt;:</c> pair missing its rewrite expression. <c>theory.rewrite</c> for bad rewrite expressions. <c>theory.relation.reserved</c>
    /// when a relation is named by a rewrite-grammar reserved word (<c>this</c>). Whatever the core factories reject: identifier grammars,
    /// <c>namespace.duplicate_relation</c>, <c>namespace.dangling_reference</c>, and <c>namespace.rewrite_cycle</c> via
    /// <c>Namespace.Create</c>. <c>theory.empty</c> and <c>theory.duplicate_namespace</c> via <c>Theory.Create</c>. YAML keys are case-sensitive but
    /// namespace identity is not, so case-variant keys collapse to one identity after lowercase normalization and fail as duplicates.
    /// </returns>
    public static Result<Theory> Parse(string text) =>
        LoadDocument(text).Bind(ParseDocument);

    /// <summary>
    /// Parses the two halves of the envelope independently and accumulates them, so a bad theory name never masks namespace defects. Neither
    /// half needs anything from the other, because a namespace key is a bare name that the enclosing theory qualifies by containment rather
    /// than by string.
    /// </summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the <see cref="Theory"/>, or the accumulated failures from both halves and <c>Theory.Create</c>.</returns>
    private static Result<Theory> ParseDocument(YamlMappingNode document) =>
        Result.Apply(
            ParseName(document).Map<Func<ImmutableArray<Namespace>, (TheoryName Name, ImmutableArray<Namespace> Namespaces)>>(
                domain => namespaces => (domain, namespaces)),
            ParseNamespaces(document))
            .Bind(domain => Theory.Create(domain.Name, domain.Namespaces));

    /// <summary>Parses the document's <c>theory:</c> key, the theory's name and its key across the catalog (<see cref="TheoryName"/> owns the grammar).</summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the <see cref="TheoryName"/>, or a <c>theory.document</c> failure when the <c>theory:</c> key is missing or its value is not a scalar.</returns>
    private static Result<TheoryName> ParseName(YamlMappingNode document) =>
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        document.Children.TryGetValue(new YamlScalarNode(DocumentKeys.Name), out var name) && name is YamlScalarNode { Value: not null } scalar
            ? TheoryName.Parse(scalar.Value!)
            : Result.Failure<TheoryName>(Error.Validation(Diagnostics.ErrorCodes.Theory.Document, $"a theory document requires a '{DocumentKeys.Name}:' key naming the theory, with a scalar value"));

    /// <summary>
    /// Parses the document's <c>namespaces:</c> key, the namespace map. Its emptiness is <c>Theory.Create</c>'s call (<c>theory.empty</c>), not
    /// this adapter's. Each key is a bare <see cref="NamespaceName"/>, and the theory it belongs to is the document's own, supplied by
    /// containment.
    /// </summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the parsed namespaces, or a <c>theory.document</c> failure when the <c>namespaces:</c> key is missing or is not a mapping.</returns>
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
            // substrate fault translated at the boundary: malformed text is a modeled outcome of parsing untrusted input
            return Result.Failure<YamlMappingNode>(Error.Validation(Diagnostics.ErrorCodes.Theory.Syntax, $"malformed YAML: {ex.Message}"));
        }
        catch (ArgumentException ex)
        {
            // YamlDotNet leaks ArgumentException for shapes its representation model cannot load (e.g. "- : this"
            // dies in YamlNode.ParseNode with "current event is of an unsupported type") — same modeled outcome
            return Result.Failure<YamlMappingNode>(Error.Validation(Diagnostics.ErrorCodes.Theory.Syntax, $"malformed YAML: {ex.Message}"));
        }
    }

    private static Result<Namespace> ParseNamespace(KeyValuePair<YamlNode, YamlNode> entry)
    {
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
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
            // "file:" with no value — a namespace with no relations — parses as a plain null scalar (core-schema null forms)
            YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" or "null" or "Null" or "NULL" or "~" } => Result.Success<ImmutableArray<Relation>>([]),
            _ => Result.Failure<ImmutableArray<Relation>>(Error.Validation(Diagnostics.ErrorCodes.Theory.Namespace, "a namespace defines a sequence of relations")),
        };

    private static Result<Relation> ParseRelation(YamlNode node) =>
        node switch
        {
            // "- owner" — a bare name, implicit this (Value is never null on a node loaded from text)
            YamlScalarNode scalar => ParseRelationName(scalar.Value!).Map(relation => new Relation(relation)),
            // "- editor: this | owner" — a single-pair mapping of name to rewrite expression
            YamlMappingNode { Children.Count: 1 } mapping => ParseRewriteRelation(mapping.Children.First()),
            _ => Result.Failure<Relation>(Error.Validation(Diagnostics.ErrorCodes.Theory.Relation, "a relation is a bare name or a single '<name>: <rewrite expression>' pair")),
        };

    private static Result<Relation> ParseRewriteRelation(KeyValuePair<YamlNode, YamlNode> entry) =>
        // Key.Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        entry is { Key: YamlScalarNode name, Value: YamlScalarNode expression }
            ? Result.Apply(
                ParseRelationName(name.Value!)
                    .Map<Func<SubjectSetRewrite, Relation>>(relation => rewrite => new Relation(relation, rewrite)),
                ParseRewriteExpression(name, expression))
            : Result.Failure<Relation>(Error.Validation(Diagnostics.ErrorCodes.Theory.Relation, "a relation is a bare name or a single '<name>: <rewrite expression>' pair"));

    /// <summary>
    /// Parses the value side of a <c>&lt;name&gt;: &lt;rewrite expression&gt;</c> pair. A missing value (<c>- viewer:</c>) loads as a plain
    /// empty scalar. A plain scalar cannot spell an empty string, so this shape is always a forgotten expression and gets a pointed error
    /// rather than the mini-language's generic unexpected-end-of-input. Any other scalar hands its raw text to the expression parser, because
    /// the theory document owns the text rather than YAML's scalar typing. A plain <c>null</c> is the identifier <c>null</c>, which keeps a relation so
    /// named round-tripping, because the renderer emits it unquoted.
    /// </summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the parsed <c>SubjectSetRewrite</c>, or a <c>theory.relation</c> failure when the expression is missing, or the expression parser's failures.</returns>
    private static Result<SubjectSetRewrite> ParseRewriteExpression(YamlScalarNode name, YamlScalarNode expression) =>
        expression is { Style: ScalarStyle.Plain, Value: null or "" }
            ? Result.Failure<SubjectSetRewrite>(Error.Validation(Diagnostics.ErrorCodes.Theory.Relation, $"relation '{name.Value}' is missing its rewrite expression; a relation without a rewrite is a bare name"))
            : RewriteExpressionParser.Parse(expression.Value!);

    /// <summary>
    /// Parses a relation name and rejects the rewrite-grammar reserved word <c>this</c>. The name <c>this</c> always lexes as the keyword, so a relation
    /// so named could never be referenced, and a reference would silently read as direct membership. The theory document reserves it. The name stays bare, because the enclosing
    /// <see cref="Namespace"/> supplies the qualification.
    /// </summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the <see cref="RelationName"/>, a <c>theory.relation.reserved</c> failure when the name is a
    /// rewrite-grammar reserved word, or the identifier-grammar failures (<c>relation_name.empty</c>, <c>relation_name.invalid</c>) that
    /// <c>RelationName.Parse</c> raises.
    /// </returns>
    private static Result<RelationName> ParseRelationName(string name) =>
        RelationName.Parse(name).Bind(relation => RewriteExpressionPrinter.IsReserved(relation)
            ? Result.Failure<RelationName>(Error.Validation(Diagnostics.ErrorCodes.Theory.RelationReserved, $"'{relation}' is reserved by the rewrite grammar and cannot name a relation in a theory document"))
            : Result.Success(relation));
}
