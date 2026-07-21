using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using static Kingo.Schemas.Tests.TestHelpers;

namespace Kingo.Schemas.Tests;

public sealed class SubjectSetRewriteTests
{
    // ---- ThisRewrite ----

    [Fact]
    public void ThisRewrite_Default_ReturnsSameInstanceOnRepeatedAccess() => Assert.Same(ThisRewrite.Default, ThisRewrite.Default);

    // ---- ComputedSubjectSetRewrite ----

    [Fact]
    public void ComputedSubjectSetRewrite_SameRelationship_AreEqual()
    {
        var a = Computed("editor");
        var b = Computed("editor");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ComputedSubjectSetRewrite_DifferentRelationship_NotEqual()
    {
        var a = Computed("editor");
        var b = Computed("viewer");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ComputedSubjectSetRewrite_ExposesRelationship()
    {
        var rewrite = Computed("editor");

        Assert.Equal(Rel("editor"), rewrite.Relationship);
    }

    // ---- FactToSubjectSetRewrite ----

    [Fact]
    public void FactToSubjectSetRewrite_SameComponents_AreEqual()
    {
        var a = FactTo("parent", "viewer");
        var b = FactTo("parent", "viewer");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FactToSubjectSetRewrite_SwappedComponents_NotEqual()
    {
        var a = FactTo("parent", "viewer");
        var swapped = FactTo("viewer", "parent");

        Assert.NotEqual(a, swapped);
    }

    [Fact]
    public void FactToSubjectSetRewrite_ExposesComponentsInDeclaredOrder()
    {
        var rewrite = FactTo("parent", "viewer");

        Assert.Equal(Rel("parent"), rewrite.FactsetRelationship);
        Assert.Equal(Rel("viewer"), rewrite.ComputedSubjectSetRelationship);
    }

    // ---- UnionRewrite ----

    [Fact]
    public void UnionRewrite_SeparatelyConstructedEqualChildren_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<SubjectSetRewrite> left = [Computed("editor"), ThisRewrite.Default];
        ImmutableArray<SubjectSetRewrite> right = [Computed("editor"), ThisRewrite.Default];

        var a = Union(left);
        var b = Union(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void UnionRewrite_DifferentOrder_NotEqual()
    {
        var a = Union([Computed("editor"), ThisRewrite.Default]);
        var b = Union([ThisRewrite.Default, Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void UnionRewrite_DifferentLength_NotEqual()
    {
        var a = Union([Computed("editor"), ThisRewrite.Default]);
        var b = Union([Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void UnionRewrite_Null_IsFalse()
    {
        var a = Union([ThisRewrite.Default]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void UnionRewrite_Create_EmptyChildren_ReturnsValidationFailure()
    {
        var result = UnionRewrite.Create([]);

        var failure = Assert.IsType<Result<UnionRewrite>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rewrite.union.empty", error.Code);
    }

    [Fact]
    public void UnionRewrite_Create_DefaultChildren_ReturnsValidationFailure()
    {
        var result = UnionRewrite.Create(default);

        var failure = Assert.IsType<Result<UnionRewrite>.Failure>(result);
        Assert.Equal("rewrite.union.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void UnionRewrite_Create_ChildrenAreCarriedOntoTheRewrite()
    {
        ImmutableArray<SubjectSetRewrite> children = [ThisRewrite.Default, Computed("editor")];

        Assert.Equal(children, Union(children).Children);
    }

    // ---- IntersectionRewrite ----

    [Fact]
    public void IntersectionRewrite_SeparatelyConstructedEqualChildren_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<SubjectSetRewrite> left = [Computed("editor"), ThisRewrite.Default];
        ImmutableArray<SubjectSetRewrite> right = [Computed("editor"), ThisRewrite.Default];

        var a = Intersection(left);
        var b = Intersection(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void IntersectionRewrite_DifferentOrder_NotEqual()
    {
        var a = Intersection([Computed("editor"), ThisRewrite.Default]);
        var b = Intersection([ThisRewrite.Default, Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void IntersectionRewrite_DifferentLength_NotEqual()
    {
        var a = Intersection([Computed("editor"), ThisRewrite.Default]);
        var b = Intersection([Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void IntersectionRewrite_Null_IsFalse()
    {
        var a = Intersection([ThisRewrite.Default]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void IntersectionRewrite_Create_EmptyChildren_ReturnsValidationFailure()
    {
        var result = IntersectionRewrite.Create([]);

        var failure = Assert.IsType<Result<IntersectionRewrite>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rewrite.intersection.empty", error.Code);
    }

    [Fact]
    public void IntersectionRewrite_Create_DefaultChildren_ReturnsValidationFailure()
    {
        var result = IntersectionRewrite.Create(default);

        var failure = Assert.IsType<Result<IntersectionRewrite>.Failure>(result);
        Assert.Equal("rewrite.intersection.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void IntersectionRewrite_Create_ChildrenAreCarriedOntoTheRewrite()
    {
        ImmutableArray<SubjectSetRewrite> children = [ThisRewrite.Default, Computed("editor")];

        Assert.Equal(children, Intersection(children).Children);
    }

    // ---- Cross-type ----

    [Fact]
    public void UnionAndIntersection_IdenticalChildren_AreNotEqual()
    {
        SubjectSetRewrite union = Union([Computed("editor")]);
        SubjectSetRewrite intersection = Intersection([Computed("editor")]);

        Assert.NotEqual(union, intersection);
        Assert.False(union.Equals((object)intersection));
    }

    // ---- ExclusionRewrite ----

    [Fact]
    public void ExclusionRewrite_SameComponents_AreEqual()
    {
        var a = Exclusion(ThisRewrite.Default, Computed("banned"));
        var b = Exclusion(ThisRewrite.Default, Computed("banned"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ExclusionRewrite_SwappedIncludeExclude_NotEqual()
    {
        var include = Computed("member");
        var exclude = Computed("banned");

        var a = Exclusion(include, exclude);
        var swapped = Exclusion(exclude, include);

        Assert.NotEqual(a, swapped);
    }

    [Fact]
    public void ExclusionRewrite_ExposesIncludeAndExcludeInDeclaredOrder()
    {
        var include = Computed("member");
        var exclude = Computed("banned");

        var rewrite = Exclusion(include, exclude);

        Assert.Equal(include, rewrite.Include);
        Assert.Equal(exclude, rewrite.Exclude);
    }

    // ---- Nested composites ----

    [Fact]
    public void NestedComposite_IndependentlyConstructedIdenticalTrees_AreEqualWithMatchingHashCodes()
    {
        var a = Union(
        [
            Computed("a"),
            Exclusion(ThisRewrite.Default, Computed("b")),
        ]);
        var b = Union(
        [
            Computed("a"),
            Exclusion(ThisRewrite.Default, Computed("b")),
        ]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NestedComposite_OneLeafDifferenceDeepInTree_NotEqual()
    {
        var a = Union(
        [
            Computed("a"),
            Exclusion(ThisRewrite.Default, Computed("b")),
        ]);
        var b = Union(
        [
            Computed("a"),
            Exclusion(ThisRewrite.Default, Computed("c")),
        ]);

        Assert.NotEqual(a, b);
    }

    // ---- Depth bound ----

    [Fact]
    public void Depth_Leaves_AreDepthOne()
    {
        Assert.Equal(1, ThisRewrite.Default.Depth);
        Assert.Equal(1, Computed("editor").Depth);
        Assert.Equal(1, FactTo("parent", "viewer").Depth);
    }

    [Fact]
    public void Depth_OperatorNodes_AreOneMoreThanTheirDeepestChild()
    {
        var exclusion = Exclusion(ThisRewrite.Default, Computed("banned"));

        Assert.Equal(2, exclusion.Depth);
        Assert.Equal(3, Union([ThisRewrite.Default, exclusion]).Depth);
        Assert.Equal(3, Intersection([exclusion, ThisRewrite.Default]).Depth);
    }

    [Fact]
    public void ExclusionRewrite_Create_PastTheDepthBound_ReturnsValidationFailure()
    {
        var result = ExclusionRewrite.Create(NestedToTheBound(), ThisRewrite.Default);

        var failure = Assert.IsType<Result<ExclusionRewrite>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rewrite.depth", error.Code);
    }

    [Fact]
    public void UnionRewrite_Create_PastTheDepthBound_ReturnsValidationFailure()
    {
        var result = UnionRewrite.Create([NestedToTheBound()]);

        var failure = Assert.IsType<Result<UnionRewrite>.Failure>(result);
        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void IntersectionRewrite_Create_PastTheDepthBound_ReturnsValidationFailure()
    {
        var result = IntersectionRewrite.Create([NestedToTheBound()]);

        var failure = Assert.IsType<Result<IntersectionRewrite>.Failure>(result);
        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    /// <summary>The deepest constructible tree — an exclusion chain whose <c>Depth</c> is exactly <see cref="SubjectSetRewrite.MaxDepth"/>.</summary>
    private static SubjectSetRewrite NestedToTheBound() =>
        Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate((SubjectSetRewrite)ThisRewrite.Default, (accumulated, _) => Exclusion(accumulated, ThisRewrite.Default));

    // ---- Exhaustive pattern match ----

    [Fact]
    public void PatternMatch_OverEveryVariant_DistinguishesEachAtMatchSites()
    {
        List<SubjectSetRewrite> variants =
        [
            ThisRewrite.Default,
            Computed("editor"),
            FactTo("parent", "viewer"),
            Union([ThisRewrite.Default]),
            Intersection([ThisRewrite.Default]),
            Exclusion(ThisRewrite.Default, ThisRewrite.Default),
        ];

        var labels = variants.Select(rewrite => rewrite switch
        {
            ThisRewrite => "this",
            ComputedSubjectSetRewrite => "computed",
            FactToSubjectSetRewrite => "fact-to",
            UnionRewrite => "union",
            IntersectionRewrite => "intersection",
            ExclusionRewrite => "exclusion",
            _ => throw new InvalidOperationException("unreachable: the union is closed"),
        }).ToList();

        string[] expected = ["this", "computed", "fact-to", "union", "intersection", "exclusion"];
        Assert.Equal(expected, labels);
    }
}
