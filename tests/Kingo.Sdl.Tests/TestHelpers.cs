using Kingo.Schemas;
using Results;
using System.Collections.Immutable;

namespace Kingo.Sdl.Tests;

/// <summary>Shared construction and unwrap helpers for the SDL adapter tests — import with <c>using static</c>.</summary>
internal static class TestHelpers
{
    /// <summary>The spec name every fixture document carries unless it is testing the name itself.</summary>
    public const string DefaultSpecName = "test";

    public static NamespacePath Ns(string value) => NamespacePath.Unchecked(value);

    public static RelationshipPath Rel(string value) => RelationshipPath.Unchecked(value);

    public static SpecPath SpecId(string value) => SpecPath.Unchecked(value);

    public static Relationship Bare(string name) => new(Rel(name));

    public static ComputedSubjectSetRewrite Computed(string name) => ComputedSubjectSetRewrite.Create(Rel(name));

    public static FactToSubjectSetRewrite FactTo(string factset, string computed) =>
        FactToSubjectSetRewrite.Create(Rel(factset), Rel(computed));

    public static UnionRewrite Union(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<UnionRewrite>.Success>(UnionRewrite.Create(children)).Value;

    public static IntersectionRewrite Intersection(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<IntersectionRewrite>.Success>(IntersectionRewrite.Create(children)).Value;

    public static ExclusionRewrite Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        Assert.IsType<Result<ExclusionRewrite>.Success>(ExclusionRewrite.Create(include, exclude)).Value;

    public static Namespace MakeNs(NamespacePath name, ImmutableArray<Relationship> relationships) =>
        Assert.IsType<Result<Namespace>.Success>(Namespace.Create(name, relationships)).Value;

    public static Spec MakeSpec(ImmutableArray<Namespace> namespaces) =>
        MakeSpec(SpecId(DefaultSpecName), namespaces);

    public static Spec MakeSpec(SpecPath name, ImmutableArray<Namespace> namespaces) =>
        Assert.IsType<Result<Spec>.Success>(Spec.Create(name, namespaces)).Value;

    /// <summary>
    /// Wraps a namespace-map fragment in the SDL document envelope — the <c>spec:</c> name plus the <c>namespaces:</c> key — so a
    /// fixture can state only the part it is about. Tests of the envelope itself pass whole documents to <see cref="ParseSuccess"/> /
    /// <see cref="ParseFailure"/> directly.
    /// </summary>
    public static string Document(string namespaceMap, string name = DefaultSpecName) =>
        $"spec: {name}\nnamespaces:\n{Indent(namespaceMap)}";

    private static string Indent(string text) =>
        string.Join('\n', text.Split('\n').Select(line => line.Length == 0 ? line : $"  {line}"));

    public static Spec ParseSuccess(string text) =>
        Assert.IsType<Result<Spec>.Success>(SpecParser.Parse(text)).Value;

    public static ImmutableArray<Error> ParseFailure(string text) =>
        Assert.IsType<Result<Spec>.Failure>(SpecParser.Parse(text)).Errors;
}
