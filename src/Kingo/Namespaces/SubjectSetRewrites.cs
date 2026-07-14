using System.Collections.Immutable;

namespace Kingo.Namespaces;

/// <summary>
/// The rewrite algebra — a closed discriminated union describing how a relationship's effective subject set is computed: direct membership (<see cref="ThisRewrite"/>), another relationship on the same resource (<see cref="ComputedSubjectSetRewrite"/>), a walk through a tupleset (<see cref="TupleToSubjectSetRewrite"/>), and the set operators (<see cref="UnionRewrite"/>, <see cref="IntersectionRewrite"/>, <see cref="ExclusionRewrite"/>). Parse-agnostic: produced equally by the PDL adapter, other serialization adapters, or the Write API. Authoring syntax and precedence: docs/notes/pdl-yaml.md.
/// </summary>
public abstract record SubjectSetRewrite
{
    private protected SubjectSetRewrite() { }
}

/// <summary>Direct membership: the subjects written in statements for this relationship.</summary>
public sealed record ThisRewrite
    : SubjectSetRewrite
{
    /// <summary>The singleton instance — the rewrite is stateless.</summary>
    public static ThisRewrite Default { get; } = new();
}

/// <summary>The subject set of another relationship on the same resource (e.g. <c>viewer</c> includes <c>editor</c>).</summary>
public sealed record ComputedSubjectSetRewrite(
    RelationshipIdentifier Relationship)
    : SubjectSetRewrite;

/// <summary>
/// Walks the statements of <paramref name="TuplesetRelationship"/> on the resource and, for each subject found, evaluates <paramref name="ComputedRelationship"/> on that subject — Zanzibar's mechanism for inherited permissions (e.g. "viewer on the parent folder grants viewer on the file").
/// </summary>
public sealed record TupleToSubjectSetRewrite(
    RelationshipIdentifier TuplesetRelationship,
    RelationshipIdentifier ComputedRelationship)
    : SubjectSetRewrite;

/// <summary>Union of the child rewrites' subject sets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
public sealed record UnionRewrite(
    ImmutableArray<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    public bool Equals(UnionRewrite? other) =>
        other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

    public override int GetHashCode() => RewriteHash.OfSequence(Children);
}

/// <summary>Intersection of the child rewrites' subject sets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
public sealed record IntersectionRewrite(
    ImmutableArray<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    public bool Equals(IntersectionRewrite? other) =>
        other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

    public override int GetHashCode() => RewriteHash.OfSequence(Children);
}

/// <summary>The subjects of <paramref name="Include"/> excluding the subjects of <paramref name="Exclude"/>.</summary>
public sealed record ExclusionRewrite(
    SubjectSetRewrite Include,
    SubjectSetRewrite Exclude)
    : SubjectSetRewrite;

internal static class RewriteHash
{
    public static int OfSequence<T>(ImmutableArray<T> items)
    {
        var hash = new HashCode();
        foreach (var item in items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
