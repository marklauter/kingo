using Kingo.Namespaces;
using Results;
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

    [Fact]
    public void Define_UniqueRelationshipNames_ReturnsSuccessEqualToConstructed()
    {
        var result = Namespace.Define(Ns("doc"), [Rel("viewer"), Rel("editor")]);

        var success = Assert.IsType<Result<Namespace>.Success>(result);
        Assert.Equal(new Namespace(Ns("doc"), [Rel("viewer"), Rel("editor")]), success.Value);
    }

    [Fact]
    public void Define_EmptyRelationships_ReturnsSuccess()
    {
        var result = Namespace.Define(Ns("doc"), []);

        var success = Assert.IsType<Result<Namespace>.Success>(result);
        Assert.Empty(success.Value.Relationships);
    }

    [Fact]
    public void Define_DuplicateRelationshipName_ReturnsValidationFailure()
    {
        var result = Namespace.Define(Ns("doc"), [Rel("viewer"), Rel("editor"), Rel("viewer")]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace.duplicate_relationship", error.Code);
        Assert.Contains("'viewer'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'doc'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Define_SameNameDifferentRewrites_IsStillADuplicate()
    {
        // Uniqueness is by Name alone — two definitions for the same relationship are
        // the conflict, regardless of whether their rewrites agree.
        var viewerDirect = new Relationship(RelationshipIdentifier.Create("viewer"));
        var viewerComputed = new Relationship(
            RelationshipIdentifier.Create("viewer"),
            new ComputedSubjectSetRewrite(RelationshipIdentifier.Create("editor")));

        var result = Namespace.Define(Ns("doc"), [viewerDirect, viewerComputed]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("namespace.duplicate_relationship", error.Code);
    }

    [Fact]
    public void Define_MultipleDuplicatedNames_AccumulatesOneErrorPerNameInFirstOccurrenceOrder()
    {
        var result = Namespace.Define(
            Ns("doc"),
            [Rel("viewer"), Rel("editor"), Rel("viewer"), Rel("editor")]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("namespace.duplicate_relationship", error.Code));
        Assert.Contains("'viewer'", failure.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("'editor'", failure.Errors[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Define_NamesDifferingOnlyByCase_AreDistinct()
    {
        // Uniqueness is ordinal over canonical values. Parsed relationship names are
        // always lowercase; mixed case here is only reachable through the trusted
        // Create path, and Define compares what it is given.
        var result = Namespace.Define(Ns("doc"), [Rel("viewer"), Rel("Viewer")]);

        _ = Assert.IsType<Result<Namespace>.Success>(result);
    }

    [Fact]
    public void Constructor_IsPureAssignment_AcceptsDuplicatesDefineRejects()
    {
        // Trusted path — mirrors IValue.Create: no validation, misuse is the caller's defect.
        var ns = new Namespace(Ns("doc"), [Rel("viewer"), Rel("viewer")]);

        Assert.Equal(2, ns.Relationships.Length);
    }
}
