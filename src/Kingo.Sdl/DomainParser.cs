using Kingo.Domains;
using Results;
using System.Collections.Immutable;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Kingo.Sdl;

/// <summary>
/// The parse half of the SDL adapter: Schema Definition Language document text ([[specs]]) to the core domain model
/// (<see cref="DomainPrinter.Print"/> renders the other direction). YAML carries the domain name and the outer namespace map. Each relationship's
/// optional rewrite expression is an embedded mini-language handled by <see cref="RewriteExpressionParser"/> and
/// <see cref="RewriteExpressionPrinter"/>. Parsing exits through the core's validating factories (<c>RelationshipName.Parse</c>,
/// <c>NamespaceName.Parse</c>, <c>DomainName.Parse</c>, <c>Namespace.Create</c>, <c>Domain.Create</c>), accumulating every document-level,
/// identifier-level, and expression-level error into one <see cref="Result{T}"/> failure.
/// </summary>
public static class DomainParser
{
    private const string NameKey = "domain";
    private const string NamespacesKey = "namespaces";

    /// <summary>Parses untrusted SDL text into the defined <see cref="Domain"/>.</summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the defined <see cref="Domain"/>, or every accumulated validation <see cref="Error"/> in
    /// document order. <c>domain.syntax</c> for malformed YAML. <c>domain.document</c> when the text is not a single mapping, or the <c>domain:</c>
    /// or <c>namespaces:</c> keys are missing or misshapen. <c>domain.namespace</c> or <c>domain.relationship</c> for wrong node shapes, or a
    /// <c>&lt;name&gt;:</c> pair missing its rewrite expression. <c>domain.rewrite</c> for bad rewrite expressions. <c>domain.relationship.reserved</c>
    /// when a relationship is named by a rewrite-grammar reserved word (<c>this</c>). Whatever the core factories reject: identifier grammars,
    /// <c>namespace.duplicate_relationship</c>, <c>namespace.dangling_reference</c>, and <c>namespace.rewrite_cycle</c> via
    /// <c>Namespace.Create</c>. <c>domain.empty</c> and <c>domain.duplicate_namespace</c> via <c>Domain.Create</c>. YAML keys are case-sensitive but
    /// namespace identity is not, so case-variant keys collapse to one identity after lowercase normalization and fail as duplicates.
    /// </returns>
    public static Result<Domain> Parse(string text) =>
        LoadDocument(text).Bind(ParseDocument);

    /// <summary>
    /// Parses the two halves of the envelope independently and accumulates them, so a bad domain name never masks namespace defects. Neither
    /// half needs anything from the other, because a namespace key is a bare name that the enclosing domain qualifies by containment rather
    /// than by string.
    /// </summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the <see cref="Domain"/>, or the accumulated failures from both halves and <c>Domain.Create</c>.</returns>
    private static Result<Domain> ParseDocument(YamlMappingNode document) =>
        Result.Apply(
            ParseName(document).Map<Func<ImmutableArray<Namespace>, (DomainName Name, ImmutableArray<Namespace> Namespaces)>>(
                domain => namespaces => (domain, namespaces)),
            ParseNamespaces(document))
            .Bind(domain => Domain.Create(domain.Name, domain.Namespaces));

    /// <summary>Parses the document's <c>domain:</c> key, the domain's name and domain key (<see cref="DomainName"/> owns the grammar).</summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the <see cref="DomainName"/>, or a <c>domain.document</c> failure when the <c>domain:</c> key is missing or its value is not a scalar.</returns>
    private static Result<DomainName> ParseName(YamlMappingNode document) =>
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        document.Children.TryGetValue(new YamlScalarNode(NameKey), out var name) && name is YamlScalarNode { Value: not null } scalar
            ? DomainName.Parse(scalar.Value!)
            : Result.Failure<DomainName>(Error.Validation("domain.document", $"a SDL document requires a '{NameKey}:' key naming the domain, with a scalar value"));

    /// <summary>
    /// Parses the document's <c>namespaces:</c> key, the namespace map. Its emptiness is <c>Domain.Create</c>'s call (<c>domain.empty</c>), not
    /// this adapter's. Each key is a bare <see cref="NamespaceName"/>, and the domain it belongs to is the document's own, supplied by
    /// containment.
    /// </summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the parsed namespaces, or a <c>domain.document</c> failure when the <c>namespaces:</c> key is missing or is not a mapping.</returns>
    private static Result<ImmutableArray<Namespace>> ParseNamespaces(YamlMappingNode document) =>
        document.Children.TryGetValue(new YamlScalarNode(NamespacesKey), out var namespaces) && namespaces is YamlMappingNode map
            ? map.Children.Select(ParseNamespace).Sequence()
            : Result.Failure<ImmutableArray<Namespace>>(Error.Validation("domain.document", $"a SDL document requires a '{NamespacesKey}:' key mapping namespace name to relationship list"));

    private static Result<YamlMappingNode> LoadDocument(string text)
    {
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(text);
            stream.Load(reader);
            return stream.Documents is [{ RootNode: YamlMappingNode document }]
                ? Result.Success(document)
                : Result.Failure<YamlMappingNode>(Error.Validation("domain.document", $"a SDL document is a single YAML mapping carrying a '{NameKey}:' name and a '{NamespacesKey}:' map"));
        }
        catch (YamlException ex)
        {
            // substrate fault translated at the boundary: malformed text is a modeled outcome of parsing untrusted input
            return Result.Failure<YamlMappingNode>(Error.Validation("domain.syntax", $"malformed YAML: {ex.Message}"));
        }
        catch (ArgumentException ex)
        {
            // YamlDotNet leaks ArgumentException for shapes its representation model cannot load (e.g. "- : this"
            // dies in YamlNode.ParseNode with "current event is of an unsupported type") — same modeled outcome
            return Result.Failure<YamlMappingNode>(Error.Validation("domain.syntax", $"malformed YAML: {ex.Message}"));
        }
    }

    private static Result<Namespace> ParseNamespace(KeyValuePair<YamlNode, YamlNode> entry)
    {
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        var name = entry.Key is YamlScalarNode key
            ? NamespaceName.Parse(key.Value!)
            : Result.Failure<NamespaceName>(Error.Validation("domain.namespace", "a namespace name must be a scalar"));

        return Result.Apply(
            name.Map<Func<ImmutableArray<Relationship>, (NamespaceName Name, ImmutableArray<Relationship> Relationships)>>(n => relationships => (n, relationships)),
            ParseRelationships(entry.Value))
            .Bind(ns => Namespace.Create(ns.Name, ns.Relationships));
    }

    private static Result<ImmutableArray<Relationship>> ParseRelationships(YamlNode node) =>
        node switch
        {
            YamlSequenceNode sequence => sequence.Children.Select(ParseRelationship).Sequence(),
            // "file:" with no value — a namespace with no relationships — parses as a plain null scalar (core-schema null forms)
            YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" or "null" or "Null" or "NULL" or "~" } => Result.Success<ImmutableArray<Relationship>>([]),
            _ => Result.Failure<ImmutableArray<Relationship>>(Error.Validation("domain.namespace", "a namespace defines a sequence of relationships")),
        };

    private static Result<Relationship> ParseRelationship(YamlNode node) =>
        node switch
        {
            // "- owner" — a bare name, implicit this (Value is never null on a node loaded from text)
            YamlScalarNode scalar => ParseRelationshipName(scalar.Value!).Map(relationship => new Relationship(relationship)),
            // "- editor: this | owner" — a single-pair mapping of name to rewrite expression
            YamlMappingNode { Children.Count: 1 } mapping => ParseRewriteRelationship(mapping.Children.First()),
            _ => Result.Failure<Relationship>(Error.Validation("domain.relationship", "a relationship is a bare name or a single '<name>: <rewrite expression>' pair")),
        };

    private static Result<Relationship> ParseRewriteRelationship(KeyValuePair<YamlNode, YamlNode> entry) =>
        // Key.Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        entry is { Key: YamlScalarNode name, Value: YamlScalarNode expression }
            ? Result.Apply(
                ParseRelationshipName(name.Value!)
                    .Map<Func<SubjectSetRewrite, Relationship>>(relationship => rewrite => new Relationship(relationship, rewrite)),
                ParseRewriteExpression(name, expression))
            : Result.Failure<Relationship>(Error.Validation("domain.relationship", "a relationship is a bare name or a single '<name>: <rewrite expression>' pair"));

    /// <summary>
    /// Parses the value side of a <c>&lt;name&gt;: &lt;rewrite expression&gt;</c> pair. A missing value (<c>- viewer:</c>) loads as a plain
    /// empty scalar. A plain scalar cannot spell an empty string, so this shape is always a forgotten expression and gets a pointed error
    /// rather than the mini-language's generic unexpected-end-of-input. Any other scalar hands its raw text to the expression parser, because
    /// SDL owns the text rather than YAML's scalar typing. A plain <c>null</c> is the identifier <c>null</c>, which keeps a relationship so
    /// named round-tripping, because the renderer emits it unquoted.
    /// </summary>
    /// <returns>A successful <see cref="Result{T}"/> carrying the parsed <c>SubjectSetRewrite</c>, or a <c>domain.relationship</c> failure when the expression is missing, or the expression parser's failures.</returns>
    private static Result<SubjectSetRewrite> ParseRewriteExpression(YamlScalarNode name, YamlScalarNode expression) =>
        expression is { Style: ScalarStyle.Plain, Value: null or "" }
            ? Result.Failure<SubjectSetRewrite>(Error.Validation("domain.relationship", $"relationship '{name.Value}' is missing its rewrite expression; a relationship without a rewrite is a bare name"))
            : RewriteExpressionParser.Parse(expression.Value!);

    /// <summary>
    /// Parses a relationship name and rejects the rewrite-grammar reserved word <c>this</c>. The name <c>this</c> always lexes as the keyword, so a relationship
    /// so named could never be referenced, and a reference would silently read as direct membership. SDL reserves it. The name stays bare, because the enclosing
    /// <see cref="Namespace"/> supplies the qualification.
    /// </summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the <see cref="RelationshipName"/>, a <c>domain.relationship.reserved</c> failure when the name is a
    /// rewrite-grammar reserved word, or the identifier-grammar failures (<c>relationship_name.empty</c>, <c>relationship_name.invalid</c>) that
    /// <c>RelationshipName.Parse</c> raises.
    /// </returns>
    private static Result<RelationshipName> ParseRelationshipName(string name) =>
        RelationshipName.Parse(name).Bind(relationship => RewriteExpressionPrinter.IsReserved(relationship)
            ? Result.Failure<RelationshipName>(Error.Validation("domain.relationship.reserved", $"'{relationship}' is reserved by the rewrite grammar and cannot name a relationship in SDL"))
            : Result.Success(relationship));
}
