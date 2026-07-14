using Kingo.Namespaces;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Tests.Namespaces;

public sealed class NamespaceTests
{
    private static NamespaceIdentifier Ns(string value) => NamespaceIdentifier.Create(value);

    private static Relationship Rel(string name) => new(RelationshipIdentifier.Create(name));

    [Fact]
    public void Equals_SameNameAndElementWiseEqualRelationships_AreEqualWithMatchingHashCodes()
    {
        // Separately-constructed ImmutableArray instances with element-wise-equal contents.
        // Default record equality over ImmutableArray compares references and would fail this.
        ImmutableArray<Relationship> left = [Rel("viewer"), Rel("editor")];
        ImmutableArray<Relationship> right = [Rel("viewer"), Rel("editor")];

        var a = new Namespace(Ns("doc"), left);
        var b = new Namespace(Ns("doc"), right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentName_NotEqual()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer")]);
        var b = new Namespace(Ns("folder"), [Rel("viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_SameNameDifferentRelationships_NotEqual()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer")]);
        var b = new Namespace(Ns("doc"), [Rel("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_SameRelationshipsDifferentOrder_NotEqual()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer"), Rel("editor")]);
        var b = new Namespace(Ns("doc"), [Rel("editor"), Rel("viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentLengthsPrefix_NotEqual()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer"), Rel("editor")]);
        var b = new Namespace(Ns("doc"), [Rel("viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_BothEmptyRelationships_AreEqual()
    {
        ImmutableArray<Relationship> left = [];
        ImmutableArray<Relationship> right = [];

        var a = new Namespace(Ns("doc"), left);
        var b = new Namespace(Ns("doc"), right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void Equals_Null_IsFalse()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer")]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void With_ChangingName_ProducesUnequalValue()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer")]);

        var b = a with { Name = Ns("folder") };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void With_NoChanges_ProducesEqualValue()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer")]);

        var b = a with { };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_IsConsistentWithEquals()
    {
        var a = new Namespace(Ns("doc"), [Rel("viewer"), Rel("editor")]);
        var b = new Namespace(Ns("doc"), [Rel("viewer"), Rel("editor")]);
        var c = new Namespace(Ns("doc"), [Rel("viewer")]);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.False(a == c);
        Assert.True(a != c);
    }
}
