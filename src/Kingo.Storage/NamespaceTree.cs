using Kingo.Facts;
using Kingo.Specs;
using LanguageExt;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Kingo.Storage;

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

    private static SubjectSetRewrite ConvertRewrite(Specs.SubjectSetRewrite? rule) =>
        rule is null
            ? This.Default
            : rule switch
            {
                Kingo.Specs.This => This.Default,
                Kingo.Specs.ComputedSubjectSetRewrite computedSet => ComputedSubjectSetRewrite.From(computedSet.Relationship),
                Kingo.Specs.UnionRewrite union => UnionRewrite.From([.. union.Children.Select(NamespaceTree.ConvertRewrite)]),
                Kingo.Specs.IntersectionRewrite intersection => IntersectionRewrite.From([.. intersection.Children.Select(NamespaceTree.ConvertRewrite)]),
                Kingo.Specs.ExclusionRewrite exclusion => ExclusionRewrite.From(NamespaceTree.ConvertRewrite(exclusion.Include), NamespaceTree.ConvertRewrite(exclusion.Exclude)),
                _ => throw new System.NotSupportedException()
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
