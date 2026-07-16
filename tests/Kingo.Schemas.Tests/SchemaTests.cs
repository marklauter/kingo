using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Schemas.Tests;

public sealed class SchemaTests
{
    private static Namespace Ns(string name, params string[] relationships) =>
        Assert.IsType<Result<Namespace>.Success>(
            Namespace.Create(
                NamespaceIdentifier.Create(name),
                [.. relationships.Select(r => new Relationship(RelationshipIdentifier.Create(r)))])).Value;

    private static SchemaIdentifier Id(string name) => SchemaIdentifier.Create(name);

    private static Schema Make(ImmutableArray<Namespace> namespaces) => Make(Id("test"), namespaces);

    private static Schema Make(SchemaIdentifier name, ImmutableArray<Namespace> namespaces) =>
        Assert.IsType<Result<Schema>.Success>(Schema.Create(name, namespaces)).Value;

    [Fact]
    public void Equals_ElementWiseEqualNamespaces_AreEqualWithMatchingHashCodes()
    {
        // Separately-constructed ImmutableArray instances with element-wise-equal contents.
        // Default record equality over ImmutableArray compares references and would fail this.
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
        // the name is the schema's domain key, so it is part of the value's identity
        var a = Make(Id("acme"), [Ns("doc", "viewer")]);
        var b = Make(Id("globex"), [Ns("doc", "viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Create_Name_IsCarriedOntoTheSchema()
    {
        var schema = Make(Id("acme"), [Ns("doc", "viewer")]);

        Assert.Equal(Id("acme"), schema.Name);
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
        var result = Schema.Create(Id("test"), [Ns("doc", "viewer"), Ns("folder", "parent")]);

        var success = Assert.IsType<Result<Schema>.Success>(result);
        Assert.Equal(Make([Ns("doc", "viewer"), Ns("folder", "parent")]), success.Value);
    }

    [Fact]
    public void Create_EmptyNamespaces_ReturnsValidationFailure()
    {
        var result = Schema.Create(Id("test"), []);

        var failure = Assert.IsType<Result<Schema>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("schema.empty", error.Code);
    }

    [Fact]
    public void Create_DefaultArray_ReturnsValidationFailure()
    {
        var result = Schema.Create(Id("test"), default);

        var failure = Assert.IsType<Result<Schema>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("schema.empty", error.Code);
    }

    [Fact]
    public void Create_DuplicateNamespaceName_ReturnsValidationFailure()
    {
        var result = Schema.Create(Id("test"), [Ns("doc", "viewer"), Ns("folder"), Ns("doc", "editor")]);

        var failure = Assert.IsType<Result<Schema>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("schema.duplicate_namespace", error.Code);
        Assert.Contains("'doc'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_MultipleDuplicatedNames_AccumulatesOneErrorPerNameInFirstOccurrenceOrder()
    {
        var result = Schema.Create(Id("test"), [Ns("doc"), Ns("folder"), Ns("doc"), Ns("folder")]);

        var failure = Assert.IsType<Result<Schema>.Failure>(result);
        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("schema.duplicate_namespace", error.Code));
        Assert.Contains("'doc'", failure.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("'folder'", failure.Errors[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_NamesDifferingOnlyByCase_AreDistinct()
    {
        // Uniqueness is ordinal over canonical values. Parsed namespace names are always
        // lowercase; mixed case here is only reachable through the trusted Create path,
        // and Create compares what it is given.
        var result = Schema.Create(Id("test"), [Ns("doc"), Ns("Doc")]);

        _ = Assert.IsType<Result<Schema>.Success>(result);
    }
}
