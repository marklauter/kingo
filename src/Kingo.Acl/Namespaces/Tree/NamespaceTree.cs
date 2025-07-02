using Kingo.Acl.Namespaces.Spec;
using Kingo.Facts;
using LanguageExt;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Kingo.Acl.Namespaces.Tree;

public sealed record NamespaceTree(
    Namespace Name,
    IReadOnlyDictionary<Relationship, SubjectSetRewrite> Relationships)
{
    public static async Task<NamespaceTree> FromFileAsync(string path) =>
        FromSpec(JsonSerializer.Deserialize<NamespaceSpec>(await File.ReadAllTextAsync(path))!);

    public static NamespaceTree FromSpec(NamespaceSpec spec) =>
        new(spec.Name, spec.Relationships
            .ToDictionary(
                r => r.Name,
                r => ConvertRewrite(r.SubjectSetRewrite)
            ));

    private static SubjectSetRewrite ConvertRewrite(Spec.SubjectSetRewrite rule) =>
        rule switch
        {
            Spec.This => This.Default,
            Spec.ComputedSubjectSetRewrite computedSet => ComputedSubjectSetRewrite.From(computedSet.Relationship),
            Spec.UnionRewrite union => UnionRewrite.From([.. union.Children.Select(ConvertRewrite)]),
            Spec.IntersectionRewrite intersection => IntersectionRewrite.From([.. intersection.Children.Select(ConvertRewrite)]),
            Spec.ExclusionRewrite exclusion => ExclusionRewrite.From(ConvertRewrite(exclusion.Include), ConvertRewrite(exclusion.Exclude)),
            Spec.TupleToSubjectSetRewrite tupleToSubjectSet => TupleToSubjectSetRewrite.From(tupleToSubjectSet.TuplesetRelation, tupleToSubjectSet.ComputedSubjectSetRelation),
            _ => throw new NotSupportedException()
        };
}

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
    IReadOnlyList<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionRewrite From(SubjectSetRewrite[] children) => new(children);
}

public sealed record IntersectionRewrite(
    IReadOnlyList<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionRewrite From(SubjectSetRewrite[] children) => new(children);
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

