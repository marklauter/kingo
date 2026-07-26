using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Theories;

/// <summary>
/// The rewrite algebra: a closed discriminated union describing how the subjects of a relation are derived. The cases are direct membership
/// (<see cref="This"/>), another relation on the same resource (<see cref="ComputedSubjectSet"/>), a walk through a factset (<see cref="FactToSubjectSet"/>),
/// and the set operators (<see cref="Union"/>, <see cref="Intersection"/>, <see cref="Exclusion"/>). The cases nest under the base, and the base constructor is
/// private, so the case set is closed by the compiler, not by convention. No seventh inhabitant is declarable anywhere. The algebra is namespace-agnostic: every
/// name position holds a bare <see cref="RelationName"/>, because the namespace always comes from the resource being evaluated and never from the stored node
/// ([[identifiers]]). It is parse-agnostic, produced equally by the theory document adapter, other serialization adapters, or the Write API. Every producer constructs through
/// the static <c>Create</c> factories, or, for the stateless <see cref="This"/>, its <see cref="This.Default"/> singleton, so a rewrite that exists satisfies its
/// invariants. The operator factories return <see cref="Result{T}"/>: they refuse empty operand lists and trees past <see cref="MaxDepth"/>. The leaves return the
/// bare type, because a <c>Result</c> on a construction that cannot fail would claim a fallibility that does not exist. Records carry structural equality only.
/// Properties are get-only with no <c>init</c> setters, so a <c>with</c> expression cannot bypass the factories. The depth bound and the gate every operator
/// factory constructs through live in <c>SubjectSetRewrite.Depth.cs</c>. Authoring syntax and precedence: [[theories]].
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "SubjectSetRewrite is a discriminated union; This, ComputedSubjectSet, FactToSubjectSet, Union, Intersection, and Exclusion are its cases, nested under the closed base and deliberately public — SubjectSetRewrite.Union reads as the case it is, and the nesting is what closes the case set against a seventh inhabitant.")]
public abstract partial record SubjectSetRewrite
{
    /// <summary>Direct membership: the subjects written in facts for this relation.</summary>
    public sealed record This
        : SubjectSetRewrite
    {
        private This()
            : base(depth: 1)
        {
        }

        /// <summary>The singleton instance. The rewrite is stateless.</summary>
        public static This Default { get; } = new();
    }

    /// <summary>
    /// The subjectset of another relation on the same resource. A bare <see cref="RelationName"/>, not a path: the re-evaluation happens on the resource
    /// in hand, so that resource's namespace is the only namespace there is. Storing a path here would be a second source of truth that could disagree with it.
    /// </summary>
    public sealed record ComputedSubjectSet
        : SubjectSetRewrite
    {
        public RelationName Relation { get; }

        private ComputedSubjectSet(RelationName relation)
            : base(depth: 1)
            => Relation = relation;

        /// <summary>
        /// Constructs a computed subjectset naming <paramref name="relation"/>. The only construction path. Infallible, because <paramref name="relation"/>
        /// is already a valid name. Whether it names a defined relation is <c>Namespace.Create</c>'s check.
        /// </summary>
        /// <returns>A <see cref="ComputedSubjectSet"/> naming <paramref name="relation"/>.</returns>
        public static ComputedSubjectSet Create(RelationName relation) => new(relation);
    }

    /// <summary>
    /// A walk through a factset: the facts of <see cref="FactsetRelation"/> on the resource, and the subjects of <see cref="ComputedSubjectSetRelation"/> on
    /// each resource those facts name. This is the shape inherited permissions take: the subjects of one relation on every resource another relation's facts
    /// name. Only <c>Fact.ResourceFact</c> subjects traverse. Subject- and subjectset-shaped subjects are modeled errors ([[rewrite-interpreters]] conditions
    /// 5–6). The second relation is a computed subjectset on each resolved resource, the same construct as <see cref="ComputedSubjectSet"/>, applied to the
    /// factset's resources rather than to this resource. Both are bare <see cref="RelationName"/>s, not paths: the factset's facts are read on the resource in
    /// hand, and the computed half evaluates on whatever resource the walk arrives at, a namespace not known until the facts are read. Neither namespace can come
    /// from the stored node.
    /// </summary>
    public sealed record FactToSubjectSet
        : SubjectSetRewrite
    {
        public RelationName FactsetRelation { get; }

        public RelationName ComputedSubjectSetRelation { get; }

        private FactToSubjectSet(RelationName factsetRelation, RelationName computedSubjectSetRelation)
            : base(depth: 1)
            => (FactsetRelation, ComputedSubjectSetRelation) = (factsetRelation, computedSubjectSetRelation);

        /// <summary>
        /// Constructs a factset walk pairing <paramref name="factsetRelation"/> with <paramref name="computedSubjectSetRelation"/>. The only construction
        /// path. Infallible, because both names are already valid. The factset reference's definedness is <c>Namespace.Create</c>'s check.
        /// </summary>
        /// <returns>A <see cref="FactToSubjectSet"/> pairing <paramref name="factsetRelation"/> with <paramref name="computedSubjectSetRelation"/>.</returns>
        public static FactToSubjectSet Create(RelationName factsetRelation, RelationName computedSubjectSetRelation) =>
            new(factsetRelation, computedSubjectSetRelation);
    }

    /// <summary>Union of the child rewrites' subjectsets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
    public sealed record Union
        : SubjectSetRewrite
    {
        public ImmutableArray<SubjectSetRewrite> Children { get; }

        private Union(ImmutableArray<SubjectSetRewrite> children, int depth)
            : base(depth)
            => Children = children;

        /// <summary>Constructs a union over <paramref name="children"/>. The only construction path.</summary>
        /// <returns>
        /// A successful <see cref="Result{T}"/> carrying the <see cref="Union"/>. Otherwise a failure when <paramref name="children"/> is empty
        /// (<c>rewrite.union.empty</c>: the theory document grammar cannot produce the shape, and an empty union has no members to take, so the shape is refused rather than
        /// given semantics), or when the tree is past <see cref="SubjectSetRewrite.MaxDepth"/> (<c>rewrite.depth</c>).
        /// </returns>
        public static Result<Union> Create(ImmutableArray<SubjectSetRewrite> children) =>
            children.IsDefaultOrEmpty
                ? Result.Failure<Union>(Error.Validation(Diagnostics.ErrorCodes.Rewrite.UnionEmpty, "a union requires at least one child rewrite"))
                : BoundedAt(DepthOver(children), depth => new Union(children, depth));

        public bool Equals(Union? other) =>
            other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

        /// <summary>Mixes in <c>EqualityContract</c> so a union does not hash equal to the same-shaped intersection. The two operators carry identical children and would otherwise share a bucket in any rewrite-keyed cache.</summary>
        /// <returns>A hash code combining the equality contract with the order-sensitive hash of <see cref="Children"/>.</returns>
        public override int GetHashCode() => HashCode.Combine(EqualityContract, SequenceHash.Of(Children));
    }

    /// <summary>Intersection of the child rewrites' subjectsets. Equality is structural over <see cref="Children"/> (element-wise, order-sensitive).</summary>
    public sealed record Intersection
        : SubjectSetRewrite
    {
        public ImmutableArray<SubjectSetRewrite> Children { get; }

        private Intersection(ImmutableArray<SubjectSetRewrite> children, int depth)
            : base(depth)
            => Children = children;

        /// <summary>Constructs an intersection over <paramref name="children"/>. The only construction path.</summary>
        /// <returns>
        /// A successful <see cref="Result{T}"/> carrying the <see cref="Intersection"/>. Otherwise a failure when <paramref name="children"/> is empty
        /// (<c>rewrite.intersection.empty</c>: the theory document grammar cannot produce the shape, and the conventional reading of an empty intersection is the universal set,
        /// with everyone a member, so the shape is refused rather than given semantics), or when the tree is past <see cref="SubjectSetRewrite.MaxDepth"/>
        /// (<c>rewrite.depth</c>).
        /// </returns>
        public static Result<Intersection> Create(ImmutableArray<SubjectSetRewrite> children) =>
            children.IsDefaultOrEmpty
                ? Result.Failure<Intersection>(Error.Validation(Diagnostics.ErrorCodes.Rewrite.IntersectionEmpty, "an intersection requires at least one child rewrite"))
                : BoundedAt(DepthOver(children), depth => new Intersection(children, depth));

        public bool Equals(Intersection? other) =>
            other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

        /// <summary>Mixes in <c>EqualityContract</c> so an intersection does not hash equal to the same-shaped union. See <see cref="Union.GetHashCode"/>.</summary>
        /// <returns>A hash code combining the equality contract with the order-sensitive hash of <see cref="Children"/>.</returns>
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

        /// <summary>Constructs an exclusion of <paramref name="exclude"/> from <paramref name="include"/>. The only construction path. Both operands already satisfy their own invariants.</summary>
        /// <returns>A successful <see cref="Result{T}"/> carrying the <see cref="Exclusion"/>, or a failure when the tree is past <see cref="SubjectSetRewrite.MaxDepth"/> (<c>rewrite.depth</c>).</returns>
        public static Result<Exclusion> Create(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
            BoundedAt(DepthOver([include, exclude]), depth => new Exclusion(include, exclude, depth));
    }
}
