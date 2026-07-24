using Kingo.Domains;
using Results;
using System.Collections.Immutable;

namespace Kingo.Sdl.Tests;

/// <summary>Shared construction and unwrap helpers for the SDL adapter tests — import with <c>using static</c>.</summary>
internal static class TestHelpers
{
    /// <summary>The spec name every fixture document carries unless it is testing the name itself.</summary>
    public const string DefaultSpecName = "test";

    /// <summary>A namespace name — bare, exactly as an SDL document writes the key and as the spec tree stores it ([[identifiers]]).</summary>
    public static NamespaceName Ns(string name) => NamespaceName.Unchecked(name);

    public static RelationshipName Rel(string value) => RelationshipName.Unchecked(value);

    public static DomainName SpecId(string value) => DomainName.Unchecked(value);

    public static Relationship Bare(string name) => new(Rel(name));

    public static SubjectSetRewrite.ComputedSubjectSet Computed(string name) => SubjectSetRewrite.ComputedSubjectSet.Create(Rel(name));

    public static SubjectSetRewrite.FactToSubjectSet FactTo(string factset, string computed) =>
        SubjectSetRewrite.FactToSubjectSet.Create(Rel(factset), Rel(computed));

    public static SubjectSetRewrite.Union Union(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<SubjectSetRewrite.Union>.Success>(SubjectSetRewrite.Union.Create(children)).Value;

    public static SubjectSetRewrite.Intersection Intersection(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<SubjectSetRewrite.Intersection>.Success>(SubjectSetRewrite.Intersection.Create(children)).Value;

    public static SubjectSetRewrite.Exclusion Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        Assert.IsType<Result<SubjectSetRewrite.Exclusion>.Success>(SubjectSetRewrite.Exclusion.Create(include, exclude)).Value;

    public static Namespace MakeNs(NamespaceName name, ImmutableArray<Relationship> relationships) =>
        Assert.IsType<Result<Namespace>.Success>(Namespace.Create(name, relationships)).Value;

    public static Domain MakeSpec(ImmutableArray<Namespace> namespaces) =>
        MakeSpec(SpecId(DefaultSpecName), namespaces);

    public static Domain MakeSpec(DomainName name, ImmutableArray<Namespace> namespaces) =>
        Assert.IsType<Result<Domain>.Success>(Domain.Create(name, namespaces)).Value;

    /// <summary>
    /// Wraps a namespace-map fragment in the SDL document envelope — the <c>domain:</c> name plus the <c>namespaces:</c> key — so a
    /// fixture can state only the part it is about. Tests of the envelope itself pass whole documents to <see cref="ParseSuccess"/> /
    /// <see cref="ParseFailure"/> directly.
    /// </summary>
    public static string Document(string namespaceMap, string name = DefaultSpecName) =>
        $"domain: {name}\nnamespaces:\n{Indent(namespaceMap)}";

    private static string Indent(string text) =>
        string.Join('\n', text.Split('\n').Select(line => line.Length == 0 ? line : $"  {line}"));

    public static Domain ParseSuccess(string text) =>
        Assert.IsType<Result<Domain>.Success>(DomainParser.Parse(text)).Value;

    public static ImmutableArray<Error> ParseFailure(string text) =>
        Assert.IsType<Result<Domain>.Failure>(DomainParser.Parse(text)).Errors;
}
