using Kingo.Theories;
using Results;
using System.Collections.Immutable;

namespace Kingo.Documents.Tests;

internal static class TestHelpers
{
    public const string DefaultTheoryName = "test";

    public static NamespaceName Ns(string name) => NamespaceName.Unchecked(name);

    public static RelationName Rel(string value) => RelationName.Unchecked(value);

    public static TheoryName TheoryId(string value) => TheoryName.Unchecked(value);

    public static Relation Bare(string name) => new(Rel(name));

    public static SubjectSetRewrite.ComputedSubjectSet Computed(string name) => SubjectSetRewrite.ComputedSubjectSet.Create(Rel(name));

    public static SubjectSetRewrite.FactToSubjectSet FactTo(string factset, string computed) =>
        SubjectSetRewrite.FactToSubjectSet.Create(Rel(factset), Rel(computed));

    public static SubjectSetRewrite.Union Union(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<SubjectSetRewrite.Union>.Success>(SubjectSetRewrite.Union.Create(children)).Value;

    public static SubjectSetRewrite.Intersection Intersection(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<SubjectSetRewrite.Intersection>.Success>(SubjectSetRewrite.Intersection.Create(children)).Value;

    public static SubjectSetRewrite.Exclusion Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        Assert.IsType<Result<SubjectSetRewrite.Exclusion>.Success>(SubjectSetRewrite.Exclusion.Create(include, exclude)).Value;

    public static Namespace MakeNs(NamespaceName name, ImmutableArray<Relation> relations) =>
        Assert.IsType<Result<Namespace>.Success>(Namespace.Create(name, relations)).Value;

    public static Theory MakeTheory(ImmutableArray<Namespace> namespaces) =>
        MakeTheory(TheoryId(DefaultTheoryName), namespaces);

    public static Theory MakeTheory(TheoryName name, ImmutableArray<Namespace> namespaces) =>
        Assert.IsType<Result<Theory>.Success>(Theory.Create(name, namespaces)).Value;

    public static string Document(string namespaceMap, string name = DefaultTheoryName) =>
        $"theory: {name}\nnamespaces:\n{Indent(namespaceMap)}";

    private static string Indent(string text) =>
        string.Join('\n', text.Split('\n').Select(line => line.Length == 0 ? line : $"  {line}"));

    public static Theory ParseSuccess(string text) =>
        Assert.IsType<Result<Theory>.Success>(TheoryParser.Parse(text)).Value;

    public static ImmutableArray<Error> ParseFailure(string text) =>
        Assert.IsType<Result<Theory>.Failure>(TheoryParser.Parse(text)).Errors;
}
