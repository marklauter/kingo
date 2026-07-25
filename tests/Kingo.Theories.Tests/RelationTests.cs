using static Kingo.Theories.Tests.TestHelpers;

namespace Kingo.Theories.Tests;

public sealed class RelationTests
{
    private static RelationName Id(string value) => RelationName.Unchecked(value);

    [Fact]
    public void SecondaryCtor_DefaultsRewriteToThisDefaultSingleton()
    {
        var relation = new Relation(Id("viewer"));

        var rewrite = Assert.IsType<SubjectSetRewrite.This>(relation.Rewrite);
        Assert.Same(SubjectSetRewrite.This.Default, rewrite);
    }

    [Fact]
    public void SecondaryCtor_EqualsExplicitThisDefaultConstruction()
    {
        var implicitRewrite = new Relation(Id("viewer"));
        var explicitRewrite = new Relation(Id("viewer"), SubjectSetRewrite.This.Default);

        Assert.Equal(explicitRewrite, implicitRewrite);
    }

    [Fact]
    public void Equals_SameNameAndRewrite_AreEqual()
    {
        var a = new Relation(Id("viewer"), Computed("editor"));
        var b = new Relation(Id("viewer"), Computed("editor"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentName_NotEqual()
    {
        var a = new Relation(Id("viewer"));
        var b = new Relation(Id("editor"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentRewrite_NotEqual()
    {
        var a = new Relation(Id("viewer"), SubjectSetRewrite.This.Default);
        var b = new Relation(Id("viewer"), Computed("editor"));

        Assert.NotEqual(a, b);
    }
}
