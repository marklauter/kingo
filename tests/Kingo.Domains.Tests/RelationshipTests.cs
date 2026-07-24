using static Kingo.Domains.Tests.TestHelpers;

namespace Kingo.Domains.Tests;

public sealed class RelationshipTests
{
    private static RelationshipName Id(string value) => RelationshipName.Unchecked(value);

    [Fact]
    public void SecondaryCtor_DefaultsRewriteToThisDefaultSingleton()
    {
        var relationship = new Relationship(Id("viewer"));

        var rewrite = Assert.IsType<SubjectSetRewrite.This>(relationship.Rewrite);
        Assert.Same(SubjectSetRewrite.This.Default, rewrite);
    }

    [Fact]
    public void SecondaryCtor_EqualsExplicitThisDefaultConstruction()
    {
        var implicitRewrite = new Relationship(Id("viewer"));
        var explicitRewrite = new Relationship(Id("viewer"), SubjectSetRewrite.This.Default);

        Assert.Equal(explicitRewrite, implicitRewrite);
    }

    [Fact]
    public void Equals_SameNameAndRewrite_AreEqual()
    {
        var a = new Relationship(Id("viewer"), Computed("editor"));
        var b = new Relationship(Id("viewer"), Computed("editor"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentName_NotEqual()
    {
        var a = new Relationship(Id("viewer"));
        var b = new Relationship(Id("editor"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentRewrite_NotEqual()
    {
        var a = new Relationship(Id("viewer"), SubjectSetRewrite.This.Default);
        var b = new Relationship(Id("viewer"), Computed("editor"));

        Assert.NotEqual(a, b);
    }
}
