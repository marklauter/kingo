using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// The rewrite algebra — a closed discriminated union describing how a relationship's effective subject set is computed: direct membership
/// (<see cref="ThisRewrite"/>), another relationship on the same resource (<see cref="ComputedSubjectSetRewrite"/>), a walk through a factset
/// (<see cref="FactToSubjectSetRewrite"/>), and the set operators (<see cref="UnionRewrite"/>, <see cref="IntersectionRewrite"/>,
/// <see cref="ExclusionRewrite"/>). Parse-agnostic: produced equally by the SDL adapter, other serialization adapters, or the Write API — every producer
/// constructs through the static <c>Create</c> factories (or, for the stateless <see cref="ThisRewrite"/>, its <see cref="ThisRewrite.Default"/> singleton),
/// so a rewrite that exists satisfies its invariants. The operator factories return <see cref="Result{T}"/> — they refuse empty operand lists and trees past
/// <see cref="MaxDepth"/>; the leaves return the bare type — a <c>Result</c> on a construction that cannot fail would claim a fallibility that does not exist.
/// Records are for structural equality only: properties are get-only with no <c>init</c> setters, so a <c>with</c> expression cannot bypass the factories.
/// Authoring syntax and precedence: [[schema-definition-language]].
/// </summary>
public abstract record SubjectSetRewrite
{
    /// <summary>
    /// Upper bound on <see cref="Depth"/>, enforced by every operator factory (<c>rewrite.depth</c>): with the bound, no constructible tree can drive a
    /// recursion over the algebra — structural equality, hashing, printing, interpretation — deep enough to exhaust a stack, even a 1MB service thread's.
    /// Generous against real rewrites (a handful of levels) and distinct from the evaluator's fact-driven [[depth-bound]], which counts stored-fact
    /// re-entries at runtime, not tree shape.
    /// </summary>
    public const int MaxDepth = 100;

    /// <summary>The tree's structural height: 1 at a leaf, one more than the deepest operand at an operator node. Never exceeds <see cref="MaxDepth"/>.</summary>
    public int Depth { get; }

    private protected SubjectSetRewrite(int depth) => Depth = depth;

    /// <summary>The refusal a tree past <see cref="MaxDepth"/> receives — shared by the operator factories and the SDL parse edge, one code for one invariant.</summary>
    public static Error DepthError() =>
        Error.Validation("rewrite.depth", $"a rewrite tree deeper than {MaxDepth} levels is refused");

    /// <summary>One more than the deepest operand — the depth an operator node over <paramref name="children"/> would have.</summary>
    private protected static int DepthOver(ImmutableArray<SubjectSetRewrite> children) => 1 + children.Max(child => child.Depth);

    /// <summary>
    /// The one depth gate every operator factory constructs through: refuses past <see cref="MaxDepth"/> (<c>rewrite.depth</c>), otherwise hands
    /// <paramref name="depth"/> to <paramref name="create"/> — so the bound cannot drift between factories.
    /// </summary>
    private protected static Result<TRewrite> BoundedAt<TRewrite>(int depth, Func<int, TRewrite> create)
        where TRewrite : SubjectSetRewrite =>
        depth > MaxDepth
            ? Result.Failure<TRewrite>(DepthError())
            : Result.Success(create(depth));
}

/// <summary>Direct membership: the subjects written in facts for this relationship.</summary>
public sealed record ThisRewrite
    : SubjectSetRewrite
{
    private ThisRewrite()
        : base(depth: 1)
    {
    }

    /// <summary>The singleton instance — the rewrite is stateless.</summary>
    public static ThisRewrite Default { get; } = new();
}

/// <summary>The subject set of another relationship on the same resource (e.g. <c>viewer</c> includes <c>editor</c>).</summary>
public sealed record ComputedSubjectSetRewrite
    : SubjectSetRewrite
{
    public RelationshipIdentifier Relationship { get; }

    private ComputedSubjectSetRewrite(RelationshipIdentifier relationship)
        : base(depth: 1)
        => Relationship = relationship;

    /// <summary>The only construction path. Infallible — <paramref name="relationship"/> is already a valid identifier; whether it names a defined relationship is <c>Namespace.Create</c>'s check.</summary>
    public static ComputedSubjectSetRewrite Create(RelationshipIdentifier relationship) => new(relationship);
}

/// <summary>
/// Walks the facts of <see cref="FactsetRelationship"/> on the resource and, for each resource found, evaluates
/// <see cref="ComputedSubjectSetRelationship"/> on that resource — Zanzibar's mechanism for inherited permissions (e.g. "viewer on the parent folder grants
/// viewer on the file"). Only <c>Fact.ResourceFact</c> members traverse — subject- and subjectset-shaped members are modeled errors ([[rewrite-interpreters]]
/// conditions 5–6). The second relationship names a computed subject set on each resolved resource — the same construct as
/// <see cref="ComputedSubjectSetRewrite"/>, applied to the factset's resources rather than to this resource; the name says so.
/// </summary>
public sealed record FactToSubjectSetRewrite
    : SubjectSetRewrite
{
    public RelationshipIdentifier FactsetRelationship { get; }

    public RelationshipIdentifier ComputedSubjectSetRelationship { get; }

    private FactToSubjectSetRewrite(RelationshipIdentifier factsetRelationship, RelationshipIdentifier computedSubjectSetRelationship)
        : base(depth: 1)
        => (FactsetRelationship, ComputedSubjectSetRelationship) = (factsetRelationship, computedSubjectSetRelationship);

    /// <summary>The only construction path. Infallible — both identifiers are already valid; the factset reference's definedness is <c>Namespace.Create</c>'s check.</summary>
    public static FactToSubjectSetRewrite Create(RelationshipIdentifier factsetRelationship, RelationshipIdentifier computedSubjectSetRelationship) =>
        new(factsetRelationship, computedSubjectSetRelationship);
}

/// <summary>Union of the child rewrites' subject sets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
public sealed record UnionRewrite
    : SubjectSetRewrite
{
    public ImmutableArray<SubjectSetRewrite> Children { get; }

    private UnionRewrite(ImmutableArray<SubjectSetRewrite> children, int depth)
        : base(depth)
        => Children = children;

    /// <summary>
    /// The only construction path — refuses an empty operand list (<c>rewrite.union.empty</c>: the SDL grammar cannot produce the shape, and an empty union
    /// has no members to take, so the shape is refused rather than given semantics) and a tree past <see cref="SubjectSetRewrite.MaxDepth"/>
    /// (<c>rewrite.depth</c>).
    /// </summary>
    public static Result<UnionRewrite> Create(ImmutableArray<SubjectSetRewrite> children) =>
        children.IsDefaultOrEmpty
            ? Result.Failure<UnionRewrite>(Error.Validation("rewrite.union.empty", "a union requires at least one child rewrite"))
            : BoundedAt(DepthOver(children), depth => new UnionRewrite(children, depth));

    public bool Equals(UnionRewrite? other) =>
        other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

    public override int GetHashCode() => RewriteHash.OfSequence(Children);
}

/// <summary>Intersection of the child rewrites' subject sets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
public sealed record IntersectionRewrite
    : SubjectSetRewrite
{
    public ImmutableArray<SubjectSetRewrite> Children { get; }

    private IntersectionRewrite(ImmutableArray<SubjectSetRewrite> children, int depth)
        : base(depth)
        => Children = children;

    /// <summary>
    /// The only construction path — refuses an empty operand list (<c>rewrite.intersection.empty</c>: the SDL grammar cannot produce the shape, and the
    /// conventional reading of an empty intersection is the universal set — everyone a member — so the shape is refused rather than given semantics) and a
    /// tree past <see cref="SubjectSetRewrite.MaxDepth"/> (<c>rewrite.depth</c>).
    /// </summary>
    public static Result<IntersectionRewrite> Create(ImmutableArray<SubjectSetRewrite> children) =>
        children.IsDefaultOrEmpty
            ? Result.Failure<IntersectionRewrite>(Error.Validation("rewrite.intersection.empty", "an intersection requires at least one child rewrite"))
            : BoundedAt(DepthOver(children), depth => new IntersectionRewrite(children, depth));

    public bool Equals(IntersectionRewrite? other) =>
        other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

    public override int GetHashCode() => RewriteHash.OfSequence(Children);
}

/// <summary>The subjects of <see cref="Include"/> excluding the subjects of <see cref="Exclude"/>.</summary>
public sealed record ExclusionRewrite
    : SubjectSetRewrite
{
    public SubjectSetRewrite Include { get; }

    public SubjectSetRewrite Exclude { get; }

    private ExclusionRewrite(SubjectSetRewrite include, SubjectSetRewrite exclude, int depth)
        : base(depth)
        => (Include, Exclude) = (include, exclude);

    /// <summary>The only construction path — refuses a tree past <see cref="SubjectSetRewrite.MaxDepth"/> (<c>rewrite.depth</c>); both operands already satisfy their own invariants.</summary>
    public static Result<ExclusionRewrite> Create(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        BoundedAt(DepthOver([include, exclude]), depth => new ExclusionRewrite(include, exclude, depth));
}

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
