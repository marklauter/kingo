using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Schemas.Tests;

public sealed class SubjectSetRewriteTests
{
    private static RelationshipIdentifier Id(string value) => RelationshipIdentifier.Create(value);

    // ---- ThisRewrite ----

    [Fact]
    public void ThisRewrite_Default_ReturnsSameInstanceOnRepeatedAccess() => Assert.Same(ThisRewrite.Default, ThisRewrite.Default);

    [Fact]
    public void ThisRewrite_NewInstance_EqualsDefaultByStructuralEquality()
    {
        var fresh = new ThisRewrite();

        Assert.Equal(ThisRewrite.Default, fresh);
        Assert.Equal(ThisRewrite.Default.GetHashCode(), fresh.GetHashCode());
    }

    // ---- ComputedSubjectSetRewrite ----

    [Fact]
    public void ComputedSubjectSetRewrite_SameRelationship_AreEqual()
    {
        var a = new ComputedSubjectSetRewrite(Id("editor"));
        var b = new ComputedSubjectSetRewrite(Id("editor"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ComputedSubjectSetRewrite_DifferentRelationship_NotEqual()
    {
        var a = new ComputedSubjectSetRewrite(Id("editor"));
        var b = new ComputedSubjectSetRewrite(Id("viewer"));

        Assert.NotEqual(a, b);
    }

    // ---- TupleToSubjectSetRewrite ----

    [Fact]
    public void TupleToSubjectSetRewrite_SameComponents_AreEqual()
    {
        var a = new TupleToSubjectSetRewrite(Id("parent"), Id("viewer"));
        var b = new TupleToSubjectSetRewrite(Id("parent"), Id("viewer"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void TupleToSubjectSetRewrite_SwappedComponents_NotEqual()
    {
        var a = new TupleToSubjectSetRewrite(Id("parent"), Id("viewer"));
        var swapped = new TupleToSubjectSetRewrite(Id("viewer"), Id("parent"));

        Assert.NotEqual(a, swapped);
    }

    [Fact]
    public void TupleToSubjectSetRewrite_ExposesComponentsInDeclaredOrder()
    {
        var rewrite = new TupleToSubjectSetRewrite(Id("parent"), Id("viewer"));

        Assert.Equal(Id("parent"), rewrite.TuplesetRelationship);
        Assert.Equal(Id("viewer"), rewrite.ComputedSubjectSetRelationship);
    }

    // ---- UnionRewrite ----

    [Fact]
    public void UnionRewrite_SeparatelyConstructedEqualChildren_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<SubjectSetRewrite> left = [new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default];
        ImmutableArray<SubjectSetRewrite> right = [new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default];

        var a = new UnionRewrite(left);
        var b = new UnionRewrite(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void UnionRewrite_DifferentOrder_NotEqual()
    {
        var a = new UnionRewrite([new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default]);
        var b = new UnionRewrite([ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("editor"))]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void UnionRewrite_DifferentLength_NotEqual()
    {
        var a = new UnionRewrite([new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default]);
        var b = new UnionRewrite([new ComputedSubjectSetRewrite(Id("editor"))]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void UnionRewrite_Null_IsFalse()
    {
        var a = new UnionRewrite([ThisRewrite.Default]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void UnionRewrite_BothEmptyChildren_AreEqual()
    {
        ImmutableArray<SubjectSetRewrite> left = [];
        ImmutableArray<SubjectSetRewrite> right = [];

        var a = new UnionRewrite(left);
        var b = new UnionRewrite(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ---- IntersectionRewrite ----

    [Fact]
    public void IntersectionRewrite_SeparatelyConstructedEqualChildren_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<SubjectSetRewrite> left = [new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default];
        ImmutableArray<SubjectSetRewrite> right = [new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default];

        var a = new IntersectionRewrite(left);
        var b = new IntersectionRewrite(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void IntersectionRewrite_DifferentOrder_NotEqual()
    {
        var a = new IntersectionRewrite([new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default]);
        var b = new IntersectionRewrite([ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("editor"))]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void IntersectionRewrite_DifferentLength_NotEqual()
    {
        var a = new IntersectionRewrite([new ComputedSubjectSetRewrite(Id("editor")), ThisRewrite.Default]);
        var b = new IntersectionRewrite([new ComputedSubjectSetRewrite(Id("editor"))]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void IntersectionRewrite_Null_IsFalse()
    {
        var a = new IntersectionRewrite([ThisRewrite.Default]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void IntersectionRewrite_BothEmptyChildren_AreEqual()
    {
        ImmutableArray<SubjectSetRewrite> left = [];
        ImmutableArray<SubjectSetRewrite> right = [];

        var a = new IntersectionRewrite(left);
        var b = new IntersectionRewrite(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ---- Cross-type ----

    [Fact]
    public void UnionAndIntersection_IdenticalChildren_AreNotEqual()
    {
        SubjectSetRewrite union = new UnionRewrite([new ComputedSubjectSetRewrite(Id("editor"))]);
        SubjectSetRewrite intersection = new IntersectionRewrite([new ComputedSubjectSetRewrite(Id("editor"))]);

        Assert.NotEqual(union, intersection);
        Assert.False(union.Equals((object)intersection));
    }

    // ---- ExclusionRewrite ----

    [Fact]
    public void ExclusionRewrite_SameComponents_AreEqual()
    {
        var a = new ExclusionRewrite(ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("banned")));
        var b = new ExclusionRewrite(ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("banned")));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ExclusionRewrite_SwappedIncludeExclude_NotEqual()
    {
        var include = new ComputedSubjectSetRewrite(Id("member"));
        var exclude = new ComputedSubjectSetRewrite(Id("banned"));

        var a = new ExclusionRewrite(include, exclude);
        var swapped = new ExclusionRewrite(exclude, include);

        Assert.NotEqual(a, swapped);
    }

    [Fact]
    public void ExclusionRewrite_ExposesIncludeAndExcludeInDeclaredOrder()
    {
        var include = new ComputedSubjectSetRewrite(Id("member"));
        var exclude = new ComputedSubjectSetRewrite(Id("banned"));

        var rewrite = new ExclusionRewrite(include, exclude);

        Assert.Equal(include, rewrite.Include);
        Assert.Equal(exclude, rewrite.Exclude);
    }

    // ---- Nested composites ----

    [Fact]
    public void NestedComposite_IndependentlyConstructedIdenticalTrees_AreEqualWithMatchingHashCodes()
    {
        var a = new UnionRewrite(
        [
            new ComputedSubjectSetRewrite(Id("a")),
            new ExclusionRewrite(ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("b"))),
        ]);
        var b = new UnionRewrite(
        [
            new ComputedSubjectSetRewrite(Id("a")),
            new ExclusionRewrite(ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("b"))),
        ]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NestedComposite_OneLeafDifferenceDeepInTree_NotEqual()
    {
        var a = new UnionRewrite(
        [
            new ComputedSubjectSetRewrite(Id("a")),
            new ExclusionRewrite(ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("b"))),
        ]);
        var b = new UnionRewrite(
        [
            new ComputedSubjectSetRewrite(Id("a")),
            new ExclusionRewrite(ThisRewrite.Default, new ComputedSubjectSetRewrite(Id("c"))),
        ]);

        Assert.NotEqual(a, b);
    }

    // ---- Exhaustive pattern match ----

    [Fact]
    public void PatternMatch_OverEveryVariant_DistinguishesEachAtMatchSites()
    {
        List<SubjectSetRewrite> variants =
        [
            ThisRewrite.Default,
            new ComputedSubjectSetRewrite(Id("editor")),
            new TupleToSubjectSetRewrite(Id("parent"), Id("viewer")),
            new UnionRewrite([ThisRewrite.Default]),
            new IntersectionRewrite([ThisRewrite.Default]),
            new ExclusionRewrite(ThisRewrite.Default, ThisRewrite.Default),
        ];

        var labels = variants.Select(rewrite => rewrite switch
        {
            ThisRewrite => "this",
            ComputedSubjectSetRewrite => "computed",
            TupleToSubjectSetRewrite => "tuple-to",
            UnionRewrite => "union",
            IntersectionRewrite => "intersection",
            ExclusionRewrite => "exclusion",
            _ => throw new InvalidOperationException("unreachable: the union is closed"),
        }).ToList();

        string[] expected = ["this", "computed", "tuple-to", "union", "intersection", "exclusion"];
        Assert.Equal(expected, labels);
    }
}
