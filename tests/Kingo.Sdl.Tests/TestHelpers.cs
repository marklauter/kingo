using Kingo.Schemas;
using Results;
using System.Collections.Immutable;

namespace Kingo.Sdl.Tests;

/// <summary>Shared construction and unwrap helpers for the SDL adapter tests — import with <c>using static</c>.</summary>
internal static class TestHelpers
{
    /// <summary>The schema name every fixture document carries unless it is testing the name itself.</summary>
    public const string DefaultSchemaName = "test";

    public static NamespaceIdentifier Ns(string value) => NamespaceIdentifier.Unchecked(value);

    public static RelationshipIdentifier Rel(string value) => RelationshipIdentifier.Unchecked(value);

    public static SchemaIdentifier SchemaId(string value) => SchemaIdentifier.Unchecked(value);

    public static Relationship Bare(string name) => new(Rel(name));

    public static ComputedSubjectSetRewrite Computed(string name) => ComputedSubjectSetRewrite.Create(Rel(name));

    public static FactToSubjectSetRewrite FactTo(string factset, string computed) =>
        FactToSubjectSetRewrite.Create(Rel(factset), Rel(computed));

    public static UnionRewrite Union(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<UnionRewrite>.Success>(UnionRewrite.Create(children)).Value;

    public static IntersectionRewrite Intersection(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<IntersectionRewrite>.Success>(IntersectionRewrite.Create(children)).Value;

    public static ExclusionRewrite Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        ExclusionRewrite.Create(include, exclude);

    public static Namespace MakeNs(NamespaceIdentifier name, ImmutableArray<Relationship> relationships) =>
        Assert.IsType<Result<Namespace>.Success>(Namespace.Create(name, relationships)).Value;

    public static Schema MakeSchema(ImmutableArray<Namespace> namespaces) =>
        MakeSchema(SchemaId(DefaultSchemaName), namespaces);

    public static Schema MakeSchema(SchemaIdentifier name, ImmutableArray<Namespace> namespaces) =>
        Assert.IsType<Result<Schema>.Success>(Schema.Create(name, namespaces)).Value;

    /// <summary>
    /// Wraps a namespace-map fragment in the SDL document envelope — the <c>schema:</c> name plus the <c>namespaces:</c> key — so a
    /// fixture can state only the part it is about. Tests of the envelope itself pass whole documents to <see cref="ParseSuccess"/> /
    /// <see cref="ParseFailure"/> directly.
    /// </summary>
    public static string Document(string namespaceMap, string name = DefaultSchemaName) =>
        $"schema: {name}\nnamespaces:\n{Indent(namespaceMap)}";

    private static string Indent(string text) =>
        string.Join('\n', text.Split('\n').Select(line => line.Length == 0 ? line : $"  {line}"));

    public static Schema ParseSuccess(string text) =>
        Assert.IsType<Result<Schema>.Success>(SchemaParser.Parse(text)).Value;

    public static ImmutableArray<Error> ParseFailure(string text) =>
        Assert.IsType<Result<Schema>.Failure>(SchemaParser.Parse(text)).Errors;
}
