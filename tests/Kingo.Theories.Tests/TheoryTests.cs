using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Theories.Tests;

public sealed class TheoryTests
{
    private static Namespace Ns(string name, params string[] relations) =>
        Assert.IsType<Result<Namespace>.Success>(
            Namespace.Create(
                NamespaceName.Unchecked(name),
                [.. relations.Select(r => new Relation(RelationName.Unchecked(r)))])).Value;

    private static TheoryName Id(string name) => TheoryName.Unchecked(name);

    private static Theory Make(ImmutableArray<Namespace> namespaces) => Make(Id("test"), namespaces);

    private static Theory Make(TheoryName name, ImmutableArray<Namespace> namespaces) =>
        Assert.IsType<Result<Theory>.Success>(Theory.Create(name, namespaces)).Value;

    [Fact]
    public void Equals_ElementWiseEqualNamespaces_AreEqualWithMatchingHashCodes()
    {
        ImmutableArray<Namespace> left = [Ns("doc", "viewer"), Ns("folder", "parent")];
        ImmutableArray<Namespace> right = [Ns("doc", "viewer"), Ns("folder", "parent")];

        var a = Make(left);
        var b = Make(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentNamespaces_NotEqual()
    {
        var a = Make([Ns("doc", "viewer")]);
        var b = Make([Ns("folder", "viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_SameNamespacesDifferentOrder_NotEqual()
    {
        var a = Make([Ns("doc"), Ns("folder")]);
        var b = Make([Ns("folder"), Ns("doc")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentLengthsPrefix_NotEqual()
    {
        var a = Make([Ns("doc"), Ns("folder")]);
        var b = Make([Ns("doc")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void Equals_Null_IsFalse()
    {
        var a = Make([Ns("doc")]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void Equals_DifferentNames_NotEqual()
    {
        var a = Make(Id("acme"), [Ns("doc", "viewer")]);
        var b = Make(Id("globex"), [Ns("doc", "viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Create_Name_IsCarriedOntoTheTheory()
    {
        var domain = Make(Id("acme"), [Ns("doc", "viewer")]);

        Assert.Equal(Id("acme"), domain.Name);
    }

    [Fact]
    public void With_NoChanges_ProducesEqualValue()
    {
        var a = Make([Ns("doc", "viewer")]);

        var b = a with { };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_IsConsistentWithEquals()
    {
        var a = Make([Ns("doc"), Ns("folder")]);
        var b = Make([Ns("doc"), Ns("folder")]);
        var c = Make([Ns("doc")]);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.False(a == c);
        Assert.True(a != c);
    }

    [Fact]
    public void Create_UniqueNamespaceNames_ReturnsSuccessEqualToConstructed()
    {
        var result = Theory.Create(Id("test"), [Ns("doc", "viewer"), Ns("folder", "parent")]);

        var success = Assert.IsType<Result<Theory>.Success>(result);
        Assert.Equal(Make([Ns("doc", "viewer"), Ns("folder", "parent")]), success.Value);
    }

    [Fact]
    public void Create_EmptyNamespaces_ReturnsValidationFailure()
    {
        var result = Theory.Create(Id("test"), []);

        var failure = Assert.IsType<Result<Theory>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("theory.empty", error.Code.Value);
    }

    [Fact]
    public void Create_DefaultArray_ReturnsValidationFailure()
    {
        var result = Theory.Create(Id("test"), default);

        var failure = Assert.IsType<Result<Theory>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("theory.empty", error.Code.Value);
    }

    [Fact]
    public void Create_DuplicateNamespaceName_ReturnsValidationFailure()
    {
        var result = Theory.Create(Id("test"), [Ns("doc", "viewer"), Ns("folder"), Ns("doc", "editor")]);

        var failure = Assert.IsType<Result<Theory>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("theory.duplicate_namespace", error.Code.Value);
        Assert.Contains("'doc'", error.Message.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_MultipleDuplicatedNames_AccumulatesOneErrorPerNameInFirstOccurrenceOrder()
    {
        var result = Theory.Create(Id("test"), [Ns("doc"), Ns("folder"), Ns("doc"), Ns("folder")]);

        var failure = Assert.IsType<Result<Theory>.Failure>(result);
        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("theory.duplicate_namespace", error.Code.Value));
        Assert.Contains("'doc'", failure.Errors[0].Message.Value, StringComparison.Ordinal);
        Assert.Contains("'folder'", failure.Errors[1].Message.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_NamesDifferingOnlyByCase_AreDistinct()
    {
        var result = Theory.Create(Id("test"), [Ns("doc"), Ns("Doc")]);

        _ = Assert.IsType<Result<Theory>.Success>(result);
    }
}
