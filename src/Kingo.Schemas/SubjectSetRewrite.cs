using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Schemas;

/// <summary>
/// The rewrite algebra — a closed discriminated union describing how a relationship's effective subject set is computed: direct membership
/// (<see cref="This"/>), another relationship on the same resource (<see cref="ComputedSubjectSet"/>), a walk through a factset
/// (<see cref="FactToSubjectSet"/>), and the set operators (<see cref="Union"/>, <see cref="Intersection"/>, <see cref="Exclusion"/>). The cases nest under
/// the base and the base constructor is private, so the case set is closed by the compiler, not by convention — no seventh inhabitant is declarable anywhere.
/// The algebra is namespace-agnostic: every name position holds a bare <see cref="RelationshipName"/>, because the namespace always comes from the resource
/// being evaluated and never from the stored node ([[identifiers]]).
/// Parse-agnostic: produced equally by the SDL adapter, other serialization adapters, or the Write API — every producer constructs through the static
/// <c>Create</c> factories (or, for the stateless <see cref="This"/>, its <see cref="This.Default"/> singleton), so a rewrite that exists satisfies its
/// invariants. The operator factories return <see cref="Result{T}"/> — they refuse empty operand lists and trees past <see cref="MaxDepth"/>; the leaves return
/// the bare type — a <c>Result</c> on a construction that cannot fail would claim a fallibility that does not exist. Records are for structural equality only:
/// properties are get-only with no <c>init</c> setters, so a <c>with</c> expression cannot bypass the factories. The depth bound and the gate every operator
/// factory constructs through live in <c>SubjectSetRewrite.Depth.cs</c>. Authoring syntax and precedence: [[specs]].
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "SubjectSetRewrite is a discriminated union; This, ComputedSubjectSet, FactToSubjectSet, Union, Intersection, and Exclusion are its cases, nested under the closed base and deliberately public — SubjectSetRewrite.Union reads as the case it is, and the nesting is what closes the case set against a seventh inhabitant.")]
public abstract partial record SubjectSetRewrite
{
    /// <summary>Direct membership: the subjects written in facts for this relationship.</summary>
    public sealed record This
        : SubjectSetRewrite
    {
        private This()
            : base(depth: 1)
        {
        }

        /// <summary>The singleton instance — the rewrite is stateless.</summary>
        public static This Default { get; } = new();
    }

    /// <summary>
    /// The subject set of another relationship on the same resource (e.g. <c>viewer</c> includes <c>editor</c>). A bare
    /// <see cref="RelationshipName"/>, not a path: the re-evaluation happens on the resource in hand, so that resource's namespace is the only namespace
    /// there is. Storing a path here would be a second source of truth that could disagree with it.
    /// </summary>
    public sealed record ComputedSubjectSet
        : SubjectSetRewrite
    {
        public RelationshipName Relationship { get; }

        private ComputedSubjectSet(RelationshipName relationship)
            : base(depth: 1)
            => Relationship = relationship;

        /// <summary>The only construction path. Infallible — <paramref name="relationship"/> is already a valid name; whether it names a defined relationship is <c>Namespace.Create</c>'s check.</summary>
        public static ComputedSubjectSet Create(RelationshipName relationship) => new(relationship);
    }

    /// <summary>
    /// Walks the facts of <see cref="FactsetRelationship"/> on the resource and, for each resource found, evaluates
    /// <see cref="ComputedSubjectSetRelationship"/> on that resource — Zanzibar's mechanism for inherited permissions (e.g. "viewer on the parent folder grants
    /// viewer on the file"). Only <c>Fact.ResourceFact</c> members traverse — subject- and subjectset-shaped members are modeled errors ([[rewrite-interpreters]]
    /// conditions 5–6). The second relationship names a computed subject set on each resolved resource — the same construct as
    /// <see cref="ComputedSubjectSet"/>, applied to the factset's resources rather than to this resource; the name says so. Both are bare
    /// <see cref="RelationshipName"/>s, not paths: the factset's facts are read on the resource in hand, and the computed half evaluates on whatever resource
    /// the walk arrives at — a namespace not known until the facts are read. Neither namespace can come from the stored node.
    /// </summary>
    public sealed record FactToSubjectSet
        : SubjectSetRewrite
    {
        public RelationshipName FactsetRelationship { get; }

        public RelationshipName ComputedSubjectSetRelationship { get; }

        private FactToSubjectSet(RelationshipName factsetRelationship, RelationshipName computedSubjectSetRelationship)
            : base(depth: 1)
            => (FactsetRelationship, ComputedSubjectSetRelationship) = (factsetRelationship, computedSubjectSetRelationship);

        /// <summary>The only construction path. Infallible — both names are already valid; the factset reference's definedness is <c>Namespace.Create</c>'s check.</summary>
        public static FactToSubjectSet Create(RelationshipName factsetRelationship, RelationshipName computedSubjectSetRelationship) =>
            new(factsetRelationship, computedSubjectSetRelationship);
    }

    /// <summary>Union of the child rewrites' subject sets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
    public sealed record Union
        : SubjectSetRewrite
    {
        public ImmutableArray<SubjectSetRewrite> Children { get; }

        private Union(ImmutableArray<SubjectSetRewrite> children, int depth)
            : base(depth)
            => Children = children;

        /// <summary>
        /// The only construction path — refuses an empty operand list (<c>rewrite.union.empty</c>: the SDL grammar cannot produce the shape, and an empty union
        /// has no members to take, so the shape is refused rather than given semantics) and a tree past <see cref="SubjectSetRewrite.MaxDepth"/>
        /// (<c>rewrite.depth</c>).
        /// </summary>
        public static Result<Union> Create(ImmutableArray<SubjectSetRewrite> children) =>
            children.IsDefaultOrEmpty
                ? Result.Failure<Union>(Error.Validation("rewrite.union.empty", "a union requires at least one child rewrite"))
                : BoundedAt(DepthOver(children), depth => new Union(children, depth));

        public bool Equals(Union? other) =>
            other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

        /// <summary>Mixes in <c>EqualityContract</c> so a union does not hash equal to the same-shaped intersection — the two operators carry identical children and would otherwise share a bucket in any rewrite-keyed cache.</summary>
        public override int GetHashCode() => HashCode.Combine(EqualityContract, SequenceHash.Of(Children));
    }

    /// <summary>Intersection of the child rewrites' subject sets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
    public sealed record Intersection
        : SubjectSetRewrite
    {
        public ImmutableArray<SubjectSetRewrite> Children { get; }

        private Intersection(ImmutableArray<SubjectSetRewrite> children, int depth)
            : base(depth)
            => Children = children;

        /// <summary>
        /// The only construction path — refuses an empty operand list (<c>rewrite.intersection.empty</c>: the SDL grammar cannot produce the shape, and the
        /// conventional reading of an empty intersection is the universal set — everyone a member — so the shape is refused rather than given semantics) and a
        /// tree past <see cref="SubjectSetRewrite.MaxDepth"/> (<c>rewrite.depth</c>).
        /// </summary>
        public static Result<Intersection> Create(ImmutableArray<SubjectSetRewrite> children) =>
            children.IsDefaultOrEmpty
                ? Result.Failure<Intersection>(Error.Validation("rewrite.intersection.empty", "an intersection requires at least one child rewrite"))
                : BoundedAt(DepthOver(children), depth => new Intersection(children, depth));

        public bool Equals(Intersection? other) =>
            other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

        /// <summary>Mixes in <c>EqualityContract</c> so an intersection does not hash equal to the same-shaped union — see <see cref="Union.GetHashCode"/>.</summary>
        public override int GetHashCode() => HashCode.Combine(EqualityContract, SequenceHash.Of(Children));
    }

    /// <summary>The subjects of <see cref="Include"/> excluding the subjects of <see cref="Exclude"/>.</summary>
    public sealed record Exclusion
        : SubjectSetRewrite
    {
        public SubjectSetRewrite Include { get; }

        public SubjectSetRewrite Exclude { get; }

        private Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude, int depth)
            : base(depth)
            => (Include, Exclude) = (include, exclude);

        /// <summary>The only construction path — refuses a tree past <see cref="SubjectSetRewrite.MaxDepth"/> (<c>rewrite.depth</c>); both operands already satisfy their own invariants.</summary>
        public static Result<Exclusion> Create(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
            BoundedAt(DepthOver([include, exclude]), depth => new Exclusion(include, exclude, depth));
    }
}
