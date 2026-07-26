using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Theories;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "SubjectSetRewrite is a discriminated union; This, ComputedSubjectSet, FactToSubjectSet, Union, Intersection, and Exclusion are its cases, nested under the closed base and deliberately public — SubjectSetRewrite.Union reads as the case it is, and the nesting is what closes the case set against a seventh inhabitant.")]
public abstract partial record SubjectSetRewrite
{
    public sealed record This
        : SubjectSetRewrite
    {
        private This()
            : base(depth: 1)
        {
        }

        public static This Default { get; } = new();
    }

    public sealed record ComputedSubjectSet
        : SubjectSetRewrite
    {
        public RelationName Relation { get; }

        private ComputedSubjectSet(RelationName relation)
            : base(depth: 1)
            => Relation = relation;

        public static ComputedSubjectSet Create(RelationName relation) => new(relation);
    }

    public sealed record FactToSubjectSet
        : SubjectSetRewrite
    {
        public RelationName FactsetRelation { get; }

        public RelationName ComputedSubjectSetRelation { get; }

        private FactToSubjectSet(RelationName factsetRelation, RelationName computedSubjectSetRelation)
            : base(depth: 1)
            => (FactsetRelation, ComputedSubjectSetRelation) = (factsetRelation, computedSubjectSetRelation);

        public static FactToSubjectSet Create(RelationName factsetRelation, RelationName computedSubjectSetRelation) =>
            new(factsetRelation, computedSubjectSetRelation);
    }

    public sealed record Union
        : SubjectSetRewrite
    {
        public ImmutableArray<SubjectSetRewrite> Children { get; }

        private Union(ImmutableArray<SubjectSetRewrite> children, int depth)
            : base(depth)
            => Children = children;

        public static Result<Union> Create(ImmutableArray<SubjectSetRewrite> children) =>
            children.IsDefaultOrEmpty
                ? Result.Failure<Union>(Error.Validation(Diagnostics.ErrorCodes.Rewrite.UnionEmpty, "a union requires at least one child rewrite"))
                : BoundedAt(DepthOver(children), depth => new Union(children, depth));

        public bool Equals(Union? other) =>
            other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

        public override int GetHashCode() => HashCode.Combine(EqualityContract, SequenceHash.Of(Children));
    }

    public sealed record Intersection
        : SubjectSetRewrite
    {
        public ImmutableArray<SubjectSetRewrite> Children { get; }

        private Intersection(ImmutableArray<SubjectSetRewrite> children, int depth)
            : base(depth)
            => Children = children;

        public static Result<Intersection> Create(ImmutableArray<SubjectSetRewrite> children) =>
            children.IsDefaultOrEmpty
                ? Result.Failure<Intersection>(Error.Validation(Diagnostics.ErrorCodes.Rewrite.IntersectionEmpty, "an intersection requires at least one child rewrite"))
                : BoundedAt(DepthOver(children), depth => new Intersection(children, depth));

        public bool Equals(Intersection? other) =>
            other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

        public override int GetHashCode() => HashCode.Combine(EqualityContract, SequenceHash.Of(Children));
    }

    public sealed record Exclusion
        : SubjectSetRewrite
    {
        public SubjectSetRewrite Include { get; }

        public SubjectSetRewrite Exclude { get; }

        private Exclusion(SubjectSetRewrite include, SubjectSetRewrite exclude, int depth)
            : base(depth)
            => (Include, Exclude) = (include, exclude);

        public static Result<Exclusion> Create(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
            BoundedAt(DepthOver([include, exclude]), depth => new Exclusion(include, exclude, depth));
    }
}
