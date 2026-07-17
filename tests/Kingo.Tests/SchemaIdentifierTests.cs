using Results;

namespace Kingo.Tests;

public sealed class SchemaIdentifierTests
{
    [Theory]
    [InlineData("acme")]
    [InlineData("acme_corp")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<SchemaIdentifier>.Success>(SchemaIdentifier.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("ACME", "acme")]
    [InlineData("Acme_Corp", "acme_corp")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<SchemaIdentifier>.Success>(SchemaIdentifier.Parse(input));
        Assert.Equal(expected, s.Value.Value);
        Assert.Equal(expected, s.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_EmptyOrWhitespace_ReturnsEmptyValidationFailure(string input)
    {
        var f = Assert.IsType<Result<SchemaIdentifier>.Failure>(SchemaIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("schema_id.empty", error.Code);
    }

    [Theory]
    [InlineData("0abc")]
    [InlineData("a-b")]
    [InlineData("a.b")]
    [InlineData("a:b")]
    [InlineData("a b")]
    [InlineData("café")]
    [InlineData("#")]
    [InlineData("@")]
    public void Parse_InvalidCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<SchemaIdentifier>.Failure>(SchemaIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("schema_id.invalid", error.Code);
    }

    [Fact]
    public void Parse_AcceptsTheSameGrammarAsNamespaceIdentifier()
    {
        // schema and namespace names are the same kind of thing — authored vocabulary — and share a grammar
        // ([[domain-language]]); this pins that they do not drift apart silently
        string[] inputs = ["acme", "ACME", "_x", "a1", "0abc", "a-b", "a.b", "a b", ""];

        foreach (var input in inputs)
            Assert.Equal(
                SchemaIdentifier.Parse(input) is Result<SchemaIdentifier>.Success,
                NamespaceIdentifier.Parse(input) is Result<NamespaceIdentifier>.Success);
    }

    [Fact]
    public void Create_BypassesValidation_AcceptsRejectedInput()
    {
        var id = SchemaIdentifier.Create("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Create_DoesNotLowercase()
    {
        var id = SchemaIdentifier.Create("ACME");
        Assert.Equal("ACME", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = SchemaIdentifier.Create("acme");
        var b = SchemaIdentifier.Create("acme");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = SchemaIdentifier.Create("acme");
        var b = SchemaIdentifier.Create("globex");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = SchemaIdentifier.Create("a");
        var b = SchemaIdentifier.Create("b");

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
        Assert.Equal(0, a.CompareTo(a));

        Assert.True(a < b);
        Assert.True(a <= b);
        Assert.False(a > b);
        Assert.False(a >= b);
        Assert.True(b > a);
        Assert.True(b >= a);
    }

    [Fact]
    public void ToString_ReturnsRawValue()
    {
        var id = SchemaIdentifier.Create("acme");
        Assert.Equal("acme", id.ToString());
    }
}
