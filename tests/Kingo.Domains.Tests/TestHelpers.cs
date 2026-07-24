using Results;
using System.Collections.Immutable;

namespace Kingo.Domains.Tests;

/// <summary>Shared construction and unwrap helpers for the spec-model tests — import with <c>using static</c>.</summary>
internal static class TestHelpers
{
    public static RelationshipName Rel(string value) => RelationshipName.Unchecked(value);

    public static SubjectSetRewrite.ComputedSubjectSet Computed(string name) => SubjectSetRewrite.ComputedSubjectSet.Create(Rel(name));

    public static SubjectSetRewrite.FactToSubjectSet FactTo(string factset, string computed) =>
        SubjectSetRewrite.FactToSubjectSet.Create(Rel(factset), Rel(computed));

    public static SubjectSetRewrite.Union Union(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<SubjectSetRewrite.Union>.Success>(SubjectSetRewrite.Union.Create(children)).Value;

    public static SubjectSetRewrite.Intersection Intersection(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<SubjectSetRewrite.Intersection>.Success>(SubjectSetRewrite.Intersection.Create(children)).Value;

    public static SubjectSetRewrite.Exclusion Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        Assert.IsType<Result<SubjectSetRewrite.Exclusion>.Success>(SubjectSetRewrite.Exclusion.Create(include, exclude)).Value;
}
