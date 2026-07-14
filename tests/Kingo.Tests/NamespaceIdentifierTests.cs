using Results;

namespace Kingo.Tests;

public sealed class NamespaceIdentifierTests
{
    [Theory]
    [InlineData("doc")]
    [InlineData("user_group")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<NamespaceIdentifier>.Success>(NamespaceIdentifier.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("DOC", "doc")]
    [InlineData("User_Group", "user_group")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<NamespaceIdentifier>.Success>(NamespaceIdentifier.Parse(input));
        Assert.Equal(expected, s.Value.Value);
        Assert.Equal(expected, s.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_EmptyOrWhitespace_ReturnsEmptyValidationFailure(string input)
    {
        var f = Assert.IsType<Result<NamespaceIdentifier>.Failure>(NamespaceIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace_id.empty", error.Code);
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
        var f = Assert.IsType<Result<NamespaceIdentifier>.Failure>(NamespaceIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace_id.invalid", error.Code);
    }

    [Fact]
    public void Create_BypassesValidation_AcceptsRejectedInput()
    {
        var id = NamespaceIdentifier.Create("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Create_DoesNotLowercase()
    {
        var id = NamespaceIdentifier.Create("DOC");
        Assert.Equal("DOC", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = NamespaceIdentifier.Create("doc");
        var b = NamespaceIdentifier.Create("doc");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = NamespaceIdentifier.Create("doc");
        var b = NamespaceIdentifier.Create("user");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = NamespaceIdentifier.Create("a");
        var b = NamespaceIdentifier.Create("b");

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
        var id = NamespaceIdentifier.Create("doc");
        Assert.Equal("doc", id.ToString());
    }
}
