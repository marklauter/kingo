using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas.Tests;

/// <summary>Shared construction and unwrap helpers for the schema-model tests — import with <c>using static</c>.</summary>
internal static class TestHelpers
{
    public static RelationshipIdentifier Rel(string value) => RelationshipIdentifier.Unchecked(value);

    public static ComputedSubjectSetRewrite Computed(string name) => ComputedSubjectSetRewrite.Create(Rel(name));

    public static FactToSubjectSetRewrite FactTo(string factset, string computed) =>
        FactToSubjectSetRewrite.Create(Rel(factset), Rel(computed));

    public static UnionRewrite Union(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<UnionRewrite>.Success>(UnionRewrite.Create(children)).Value;

    public static IntersectionRewrite Intersection(ImmutableArray<SubjectSetRewrite> children) =>
        Assert.IsType<Result<IntersectionRewrite>.Success>(IntersectionRewrite.Create(children)).Value;

    public static ExclusionRewrite Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        Assert.IsType<Result<ExclusionRewrite>.Success>(ExclusionRewrite.Create(include, exclude)).Value;
}
