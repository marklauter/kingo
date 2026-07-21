using Kingo.Schemas;
using Results;
using System.Collections.Immutable;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Kingo.Sdl;

/// <summary>
/// The parse half of the SDL adapter: Schema Definition Language document text ([[schema-definition-language]]) to the core schema model
/// (<see cref="SchemaPrinter.Print"/> renders the other direction). YAML carries the schema name and the outer namespace map; each relationship's optional rewrite
/// expression is an embedded mini-language handled by <see cref="RewriteExpressionParser"/> and <see cref="RewriteExpressionPrinter"/>. Parsing exits through
/// the core's validating factories — <c>RelationshipIdentifier.Parse</c>, <c>NamespaceIdentifier.Parse</c>, <c>SchemaIdentifier.Parse</c>, <c>Namespace.Create</c>,
/// <c>Schema.Create</c> — accumulating every document-level, identifier-level, and expression-level error into one <see cref="Result{T}"/> failure.
/// </summary>
public static class SchemaParser
{
    private const string NameKey = "schema";
    private const string NamespacesKey = "namespaces";

    /// <summary>
    /// Parses untrusted SDL text, returning the defined <see cref="Schema"/> or every accumulated validation <see cref="Error"/> in document order: <c>sdl.syntax</c>
    /// (malformed YAML), <c>sdl.document</c> (not a single mapping, or missing/misshapen <c>schema:</c> / <c>namespaces:</c> keys),
    /// <c>sdl.namespace</c> / <c>sdl.relationship</c> (wrong node shapes, or a <c>&lt;name&gt;:</c> pair missing its rewrite expression),
    /// <c>sdl.relationship.reserved</c> (a relationship named by a rewrite-grammar reserved word), <c>sdl.rewrite</c> (bad rewrite expressions), plus whatever
    /// the core factories reject: identifier grammars, <c>namespace.duplicate_relationship</c> via <c>Namespace.Create</c>, and <c>schema.empty</c> /
    /// <c>schema.duplicate_namespace</c> via <c>Schema.Create</c> — YAML keys are case-sensitive but namespace identity is not, so case-variant keys collapse
    /// to one identity after lowercase normalization and fail as duplicates.
    /// </summary>
    public static Result<Schema> Parse(string text) =>
        LoadDocument(text)
            .Bind(document => Result.Apply(
                ParseName(document).Map<Func<ImmutableArray<Namespace>, (SchemaIdentifier Name, ImmutableArray<Namespace> Namespaces)>>(
                    name => namespaces => (name, namespaces)),
                ParseNamespaces(document)))
            .Bind(schema => Schema.Create(schema.Name, schema.Namespaces));

    /// <summary>The document's <c>schema:</c> key — the schema's name, and its domain key (<see cref="SchemaIdentifier"/> owns the grammar).</summary>
    private static Result<SchemaIdentifier> ParseName(YamlMappingNode document) =>
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        document.Children.TryGetValue(new YamlScalarNode(NameKey), out var name) && name is YamlScalarNode { Value: not null } scalar
            ? SchemaIdentifier.Parse(scalar.Value!)
            : Result.Failure<SchemaIdentifier>(Error.Validation("sdl.document", $"a SDL document requires a '{NameKey}:' key naming the schema, with a scalar value"));

    /// <summary>The document's <c>namespaces:</c> key — the namespace map. Its emptiness is <c>Schema.Create</c>'s call (<c>schema.empty</c>), not this adapter's.</summary>
    private static Result<ImmutableArray<Namespace>> ParseNamespaces(YamlMappingNode document) =>
        document.Children.TryGetValue(new YamlScalarNode(NamespacesKey), out var namespaces) && namespaces is YamlMappingNode map
            ? map.Children.Select(ParseNamespace).Sequence()
            : Result.Failure<ImmutableArray<Namespace>>(Error.Validation("sdl.document", $"a SDL document requires a '{NamespacesKey}:' key mapping namespace name to relationship list"));

    private static Result<YamlMappingNode> LoadDocument(string text)
    {
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(text);
            stream.Load(reader);
            return stream.Documents is [{ RootNode: YamlMappingNode document }]
                ? Result.Success(document)
                : Result.Failure<YamlMappingNode>(Error.Validation("sdl.document", "a SDL document is a single YAML mapping carrying a 'schema:' name and a 'namespaces:' map"));
        }
        catch (YamlException ex)
        {
            // substrate fault translated at the boundary: malformed text is a modeled outcome of parsing untrusted input
            return Result.Failure<YamlMappingNode>(Error.Validation("sdl.syntax", $"malformed YAML: {ex.Message}"));
        }
        catch (ArgumentException ex)
        {
            // YamlDotNet leaks ArgumentException for shapes its representation model cannot load (e.g. "- : this"
            // dies in YamlNode.ParseNode with "current event is of an unsupported type") — same modeled outcome
            return Result.Failure<YamlMappingNode>(Error.Validation("sdl.syntax", $"malformed YAML: {ex.Message}"));
        }
    }

    private static Result<Namespace> ParseNamespace(KeyValuePair<YamlNode, YamlNode> entry)
    {
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        var name = entry.Key is YamlScalarNode key
            ? NamespaceIdentifier.Parse(key.Value!)
            : Result.Failure<NamespaceIdentifier>(Error.Validation("sdl.namespace", "a namespace name must be a scalar"));

        return Result.Apply(
            name.Map<Func<ImmutableArray<Relationship>, (NamespaceIdentifier Name, ImmutableArray<Relationship> Relationships)>>(n => relationships => (n, relationships)),
            ParseRelationships(entry.Value))
            .Bind(ns => Namespace.Create(ns.Name, ns.Relationships));
    }

    private static Result<ImmutableArray<Relationship>> ParseRelationships(YamlNode node) =>
        node switch
        {
            YamlSequenceNode sequence => sequence.Children.Select(ParseRelationship).Sequence(),
            // "file:" with no value — a namespace with no relationships — parses as a plain null scalar (core-schema null forms)
            YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" or "null" or "Null" or "NULL" or "~" } => Result.Success<ImmutableArray<Relationship>>([]),
            _ => Result.Failure<ImmutableArray<Relationship>>(Error.Validation("sdl.namespace", "a namespace defines a sequence of relationships")),
        };

    private static Result<Relationship> ParseRelationship(YamlNode node) =>
        node switch
        {
            // "- owner" — a bare name, implicit this (Value is never null on a node loaded from text)
            YamlScalarNode scalar => ParseRelationshipName(scalar.Value!).Map(relationship => new Relationship(relationship)),
            // "- editor: this | owner" — a single-pair mapping of name to rewrite expression
            YamlMappingNode { Children.Count: 1 } mapping => ParseRewriteRelationship(mapping.Children.First()),
            _ => Result.Failure<Relationship>(Error.Validation("sdl.relationship", "a relationship is a bare name or a single '<name>: <rewrite expression>' pair")),
        };

    private static Result<Relationship> ParseRewriteRelationship(KeyValuePair<YamlNode, YamlNode> entry) =>
        // Key.Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        entry is { Key: YamlScalarNode name, Value: YamlScalarNode expression }
            ? Result.Apply(
                ParseRelationshipName(name.Value!)
                    .Map<Func<SubjectSetRewrite, Relationship>>(relationship => rewrite => new Relationship(relationship, rewrite)),
                ParseRewriteExpression(name, expression))
            : Result.Failure<Relationship>(Error.Validation("sdl.relationship", "a relationship is a bare name or a single '<name>: <rewrite expression>' pair"));

    /// <summary>
    /// The value side of a <c>&lt;name&gt;: &lt;rewrite expression&gt;</c> pair. A missing value (<c>- viewer:</c>) loads as a plain empty scalar — a plain
    /// scalar cannot spell an empty string, so this shape is always a forgotten expression and gets a pointed error instead of the mini-language's generic
    /// unexpected-end-of-input. Any other scalar hands its raw text to the expression parser: SDL owns the text, not YAML's scalar typing, so a plain
    /// <c>null</c> is the identifier <c>null</c> — which is also what keeps a relationship so named round-tripping, since the renderer emits it unquoted.
    /// </summary>
    private static Result<SubjectSetRewrite> ParseRewriteExpression(YamlScalarNode name, YamlScalarNode expression) =>
        expression is { Style: ScalarStyle.Plain, Value: null or "" }
            ? Result.Failure<SubjectSetRewrite>(Error.Validation("sdl.relationship", $"relationship '{name.Value}' is missing its rewrite expression; a relationship without a rewrite is a bare name"))
            : RewriteExpressionParser.Parse(expression.Value!);

    /// <summary>
    /// A relationship name in SDL must survive the rewrite grammar: <c>this</c> always lexes as the keyword (a relationship so named could never be referenced
    /// — or worse, a reference would silently mean direct membership) and <c>...</c> cannot lex at all, so both are reserved.
    /// </summary>
    private static Result<RelationshipIdentifier> ParseRelationshipName(string name) =>
        RelationshipIdentifier.Parse(name).Bind(relationship => RewriteExpressionPrinter.IsReserved(relationship)
            ? Result.Failure<RelationshipIdentifier>(Error.Validation("sdl.relationship.reserved", $"'{relationship}' is reserved by the rewrite grammar and cannot name a relationship in SDL"))
            : Result.Success(relationship));
}
