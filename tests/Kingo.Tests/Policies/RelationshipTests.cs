using Kingo.Policies;

namespace Kingo.Tests.Policies;

public sealed class RelationshipTests
{
    private static RelationshipIdentifier Id(string value) => RelationshipIdentifier.Create(value);

    [Fact]
    public void SecondaryCtor_DefaultsRewriteToThisRewriteDefaultSingleton()
    {
        var relationship = new Relationship(Id("viewer"));

        var rewrite = Assert.IsType<ThisRewrite>(relationship.Rewrite);
        Assert.Same(ThisRewrite.Default, rewrite);
    }

    [Fact]
    public void SecondaryCtor_EqualsExplicitThisRewriteDefaultConstruction()
    {
        var implicitRewrite = new Relationship(Id("viewer"));
        var explicitRewrite = new Relationship(Id("viewer"), ThisRewrite.Default);

        Assert.Equal(explicitRewrite, implicitRewrite);
    }

    [Fact]
    public void Equals_SameNameAndRewrite_AreEqual()
    {
        var a = new Relationship(Id("viewer"), new ComputedSubjectSetRewrite(Id("editor")));
        var b = new Relationship(Id("viewer"), new ComputedSubjectSetRewrite(Id("editor")));

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
        var a = new Relationship(Id("viewer"), ThisRewrite.Default);
        var b = new Relationship(Id("viewer"), new ComputedSubjectSetRewrite(Id("editor")));

        Assert.NotEqual(a, b);
    }
}
