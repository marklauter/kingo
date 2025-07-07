using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Namespaces;

public abstract record SubjectSetRewrite;

public sealed record This
    : SubjectSetRewrite
{
    public static This Default { get; } = new();
}

public sealed record ComputedSubjectSetRewrite(Relationship Relationship)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetRewrite From(Relationship relationship) => new(relationship);
}

public sealed record UnionRewrite(
    Seq<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionRewrite From(IEnumerable<SubjectSetRewrite> children) => new(Seq.createRange(children));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionRewrite From(Seq<SubjectSetRewrite> children) => new(children);
}

public sealed record IntersectionRewrite(
    Seq<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionRewrite From(IEnumerable<SubjectSetRewrite> children) => new(Seq.createRange(children));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionRewrite From(Seq<SubjectSetRewrite> children) => new(children);
}

public sealed record ExclusionRewrite(
    SubjectSetRewrite Include,
    SubjectSetRewrite Exclude)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExclusionRewrite From(SubjectSetRewrite include, SubjectSetRewrite exclude) => new(include, exclude);
}

public sealed record TupleToSubjectSetRewrite(
    Relationship TuplesetRelation,
    Relationship ComputedSubjectSetRelation)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TupleToSubjectSetRewrite From(Relationship tuplesetRelation, Relationship computedSubjectSetRelation) =>
        new(tuplesetRelation, computedSubjectSetRelation);
}

