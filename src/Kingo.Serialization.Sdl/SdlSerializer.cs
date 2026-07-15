using Kingo.Schemas;
using Results;
using System.Collections.Immutable;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Kingo.Serialization.Sdl;

/// <summary>
/// The parse half of the SDL adapter: Schema Definition Language document text (docs/notes/sdl-yaml.md) to the core schema model
/// (<see cref="SchemaSdlExtensions.ToSdl"/> renders the other direction). YAML carries the outer namespace map; each relationship's optional rewrite
/// expression is an embedded mini-language handled by <see cref="RewriteExpressionParser"/> and <see cref="RewriteExpressionRenderer"/>. Parsing exits through
/// the core's validating factories — <c>RelationshipIdentifier.Parse</c>, <c>NamespaceIdentifier.Parse</c>, <c>Namespace.Create</c>, <c>Schema.Create</c> —
/// accumulating every document-level, identifier-level, and expression-level error into one <see cref="Result{T}"/> failure.
/// </summary>
public static class SdlSerializer
{
    /// <summary>
    /// Parses untrusted SDL text, returning the defined <see cref="Schema"/> or every accumulated validation <see cref="Error"/>: <c>sdl.syntax</c>
    /// (malformed YAML), <c>sdl.document</c> (not a single mapping), <c>sdl.namespace</c> / <c>sdl.relationship</c> (wrong node shapes),
    /// <c>sdl.relationship.reserved</c> (a relationship named by a rewrite-grammar reserved word), <c>sdl.rewrite</c> (bad rewrite expressions), plus whatever
    /// the core factories reject: identifier grammars, <c>namespace.duplicate_relationship</c> via <c>Namespace.Create</c>, and <c>schema.empty</c> /
    /// <c>schema.duplicate_namespace</c> via <c>Schema.Create</c> — YAML keys are case-sensitive but namespace identity is not, so case-variant keys collapse
    /// to one identity after lowercase normalization and fail as duplicates.
    /// </summary>
    public static Result<Schema> Parse(string text) =>
        LoadDocument(text)
            .Bind(document => document.Children.Select(ParseNamespace).Sequence())
            .Bind(Schema.Create);

    private static Result<YamlMappingNode> LoadDocument(string text)
    {
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(text);
            stream.Load(reader);
            return stream.Documents is [{ RootNode: YamlMappingNode document }]
                ? Result.Success(document)
                : Result.Failure<YamlMappingNode>(Error.Validation("sdl.document", "a SDL document is a single YAML mapping of namespace name to relationship list"));
        }
        catch (YamlException ex)
        {
            // substrate fault translated at the boundary: malformed text is a modeled outcome of parsing untrusted input
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
        // Value is never null on a node loaded from text; the nullable annotation exists for hand-built nodes
        entry is { Key: YamlScalarNode name, Value: YamlScalarNode expression }
            ? Result.Apply(
                ParseRelationshipName(name.Value!)
                    .Map<Func<SubjectSetRewrite, Relationship>>(relationship => rewrite => new Relationship(relationship, rewrite)),
                RewriteExpressionParser.Parse(expression.Value!))
            : Result.Failure<Relationship>(Error.Validation("sdl.relationship", "a relationship is a bare name or a single '<name>: <rewrite expression>' pair"));

    /// <summary>
    /// A relationship name in SDL must survive the rewrite grammar: <c>this</c> always lexes as the keyword (a relationship so named could never be referenced
    /// — or worse, a reference would silently mean direct membership) and <c>...</c> cannot lex at all, so both are reserved.
    /// </summary>
    private static Result<RelationshipIdentifier> ParseRelationshipName(string name) =>
        RelationshipIdentifier.Parse(name).Bind(relationship => RewriteExpressionRenderer.IsReserved(relationship)
            ? Result.Failure<RelationshipIdentifier>(Error.Validation("sdl.relationship.reserved", $"'{relationship}' is reserved by the rewrite grammar and cannot name a relationship in SDL"))
            : Result.Success(relationship));
}
