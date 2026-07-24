using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using static Kingo.Schemas.Tests.TestHelpers;

namespace Kingo.Schemas.Tests;

public sealed class SubjectSetRewriteTests
{
    // ---- SubjectSetRewrite.This ----

    [Fact]
    public void This_Default_ReturnsSameInstanceOnRepeatedAccess() => Assert.Same(SubjectSetRewrite.This.Default, SubjectSetRewrite.This.Default);

    // ---- SubjectSetRewrite.ComputedSubjectSet ----

    [Fact]
    public void ComputedSubjectSet_SameRelationship_AreEqual()
    {
        var a = Computed("editor");
        var b = Computed("editor");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ComputedSubjectSet_DifferentRelationship_NotEqual()
    {
        var a = Computed("editor");
        var b = Computed("viewer");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ComputedSubjectSet_ExposesRelationship()
    {
        var rewrite = Computed("editor");

        Assert.Equal(Rel("editor"), rewrite.Relationship);
    }

    // ---- SubjectSetRewrite.FactToSubjectSet ----

    [Fact]
    public void FactToSubjectSet_SameComponents_AreEqual()
    {
        var a = FactTo("parent", "viewer");
        var b = FactTo("parent", "viewer");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FactToSubjectSet_SwappedComponents_NotEqual()
    {
        var a = FactTo("parent", "viewer");
        var swapped = FactTo("viewer", "parent");

        Assert.NotEqual(a, swapped);
    }

    [Fact]
    public void FactToSubjectSet_ExposesComponentsInDeclaredOrder()
    {
        var rewrite = FactTo("parent", "viewer");

        Assert.Equal(Rel("parent"), rewrite.FactsetRelationship);
        Assert.Equal(Rel("viewer"), rewrite.ComputedSubjectSetRelationship);
    }

    // ---- SubjectSetRewrite.Union ----

    [Fact]
    public void Union_SeparatelyConstructedEqualChildren_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<SubjectSetRewrite> left = [Computed("editor"), SubjectSetRewrite.This.Default];
        ImmutableArray<SubjectSetRewrite> right = [Computed("editor"), SubjectSetRewrite.This.Default];

        var a = Union(left);
        var b = Union(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Union_DifferentOrder_NotEqual()
    {
        var a = Union([Computed("editor"), SubjectSetRewrite.This.Default]);
        var b = Union([SubjectSetRewrite.This.Default, Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Union_DifferentLength_NotEqual()
    {
        var a = Union([Computed("editor"), SubjectSetRewrite.This.Default]);
        var b = Union([Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void Union_Null_IsFalse()
    {
        var a = Union([SubjectSetRewrite.This.Default]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void Union_Create_EmptyChildren_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Union.Create([]);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Union>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rewrite.union.empty", error.Code);
    }

    [Fact]
    public void Union_Create_DefaultChildren_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Union.Create(default);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Union>.Failure>(result);
        Assert.Equal("rewrite.union.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Union_Create_ChildrenAreCarriedOntoTheRewrite()
    {
        ImmutableArray<SubjectSetRewrite> children = [SubjectSetRewrite.This.Default, Computed("editor")];

        Assert.Equal(children, Union(children).Children);
    }

    // ---- SubjectSetRewrite.Intersection ----

    [Fact]
    public void Intersection_SeparatelyConstructedEqualChildren_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<SubjectSetRewrite> left = [Computed("editor"), SubjectSetRewrite.This.Default];
        ImmutableArray<SubjectSetRewrite> right = [Computed("editor"), SubjectSetRewrite.This.Default];

        var a = Intersection(left);
        var b = Intersection(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Intersection_DifferentOrder_NotEqual()
    {
        var a = Intersection([Computed("editor"), SubjectSetRewrite.This.Default]);
        var b = Intersection([SubjectSetRewrite.This.Default, Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Intersection_DifferentLength_NotEqual()
    {
        var a = Intersection([Computed("editor"), SubjectSetRewrite.This.Default]);
        var b = Intersection([Computed("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void Intersection_Null_IsFalse()
    {
        var a = Intersection([SubjectSetRewrite.This.Default]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void Intersection_Create_EmptyChildren_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Intersection.Create([]);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Intersection>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rewrite.intersection.empty", error.Code);
    }

    [Fact]
    public void Intersection_Create_DefaultChildren_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Intersection.Create(default);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Intersection>.Failure>(result);
        Assert.Equal("rewrite.intersection.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Intersection_Create_ChildrenAreCarriedOntoTheRewrite()
    {
        ImmutableArray<SubjectSetRewrite> children = [SubjectSetRewrite.This.Default, Computed("editor")];

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

    [Fact]
    public void UnionAndIntersection_IdenticalChildren_DoNotShareAHashCode()
    {
        SubjectSetRewrite union = Union([Computed("editor")]);
        SubjectSetRewrite intersection = Intersection([Computed("editor")]);

        Assert.NotEqual(union.GetHashCode(), intersection.GetHashCode());
    }

    // ---- SubjectSetRewrite.Exclusion ----

    [Fact]
    public void Exclusion_SameComponents_AreEqual()
    {
        var a = Exclusion(SubjectSetRewrite.This.Default, Computed("banned"));
        var b = Exclusion(SubjectSetRewrite.This.Default, Computed("banned"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Exclusion_SwappedIncludeExclude_NotEqual()
    {
        var include = Computed("member");
        var exclude = Computed("banned");

        var a = Exclusion(include, exclude);
        var swapped = Exclusion(exclude, include);

        Assert.NotEqual(a, swapped);
    }

    [Fact]
    public void Exclusion_ExposesIncludeAndExcludeInDeclaredOrder()
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
            Exclusion(SubjectSetRewrite.This.Default, Computed("b")),
        ]);
        var b = Union(
        [
            Computed("a"),
            Exclusion(SubjectSetRewrite.This.Default, Computed("b")),
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
            Exclusion(SubjectSetRewrite.This.Default, Computed("b")),
        ]);
        var b = Union(
        [
            Computed("a"),
            Exclusion(SubjectSetRewrite.This.Default, Computed("c")),
        ]);

        Assert.NotEqual(a, b);
    }

    // ---- Depth bound ----

    [Fact]
    public void Depth_Leaves_AreDepthOne()
    {
        Assert.Equal(1, SubjectSetRewrite.This.Default.Depth);
        Assert.Equal(1, Computed("editor").Depth);
        Assert.Equal(1, FactTo("parent", "viewer").Depth);
    }

    [Fact]
    public void Depth_OperatorNodes_AreOneMoreThanTheirDeepestChild()
    {
        var exclusion = Exclusion(SubjectSetRewrite.This.Default, Computed("banned"));

        Assert.Equal(2, exclusion.Depth);
        Assert.Equal(3, Union([SubjectSetRewrite.This.Default, exclusion]).Depth);
        Assert.Equal(3, Intersection([exclusion, SubjectSetRewrite.This.Default]).Depth);
    }

    [Fact]
    public void Exclusion_Create_PastTheDepthBound_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Exclusion.Create(NestedToTheBound(), SubjectSetRewrite.This.Default);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Exclusion>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rewrite.depth", error.Code);
    }

    [Fact]
    public void Union_Create_PastTheDepthBound_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Union.Create([NestedToTheBound()]);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Union>.Failure>(result);
        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Intersection_Create_PastTheDepthBound_ReturnsValidationFailure()
    {
        var result = SubjectSetRewrite.Intersection.Create([NestedToTheBound()]);

        var failure = Assert.IsType<Result<SubjectSetRewrite.Intersection>.Failure>(result);
        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    /// <summary>The deepest constructible tree — an exclusion chain whose <c>Depth</c> is exactly <see cref="SubjectSetRewrite.MaxDepth"/>.</summary>
    private static SubjectSetRewrite NestedToTheBound() =>
        Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate((SubjectSetRewrite)SubjectSetRewrite.This.Default, (accumulated, _) => Exclusion(accumulated, SubjectSetRewrite.This.Default));

    // ---- Exhaustive pattern match ----

    [Fact]
    public void PatternMatch_OverEveryVariant_DistinguishesEachAtMatchSites()
    {
        List<SubjectSetRewrite> variants =
        [
            SubjectSetRewrite.This.Default,
            Computed("editor"),
            FactTo("parent", "viewer"),
            Union([SubjectSetRewrite.This.Default]),
            Intersection([SubjectSetRewrite.This.Default]),
            Exclusion(SubjectSetRewrite.This.Default, SubjectSetRewrite.This.Default),
        ];

        var labels = variants.Select(rewrite => rewrite switch
        {
            SubjectSetRewrite.This => "this",
            SubjectSetRewrite.ComputedSubjectSet => "computed",
            SubjectSetRewrite.FactToSubjectSet => "fact-to",
            SubjectSetRewrite.Union => "union",
            SubjectSetRewrite.Intersection => "intersection",
            SubjectSetRewrite.Exclusion => "exclusion",
            _ => throw new InvalidOperationException("unreachable: the union is closed"),
        }).ToList();

        string[] expected = ["this", "computed", "fact-to", "union", "intersection", "exclusion"];
        Assert.Equal(expected, labels);
    }
}
