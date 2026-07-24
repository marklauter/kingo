using Results;

namespace Kingo.Tests;

public sealed class NamespacePathTests
{
    [Theory]
    [InlineData("io/doc")]
    [InlineData("io/user_group")]
    [InlineData("_x/_private")]
    [InlineData("a/a1")]
    [InlineData("a/a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<NamespacePath>.Success>(NamespacePath.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("IO/DOC", "io/doc")]
    [InlineData("Io/User_Group", "io/user_group")]
    [InlineData("A/A1", "a/a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<NamespacePath>.Success>(NamespacePath.Parse(input));
        Assert.Equal(expected, s.Value.Value);
        Assert.Equal(expected, s.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_NullEmptyOrWhitespace_ReturnsEmptyValidationFailure(string? input)
    {
        // null reaches Parse only through reflection callers (see IParse); it lands in the empty guard
        var f = Assert.IsType<Result<NamespacePath>.Failure>(NamespacePath.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace_path.empty", error.Code);
    }

    [Theory]
    [InlineData("io/0abc")]
    [InlineData("io/a-b")]
    [InlineData("io/a.b")]
    [InlineData("io/a:b")]
    [InlineData("io/a b")]
    [InlineData("io/café")]
    [InlineData("io/#")]
    [InlineData("io/@")]
    [InlineData("0io/a")]
    [InlineData("io-x/a")]
    // one segment is not a namespace: there is no namespace called 'file' ([[identifiers]])
    [InlineData("file")]
    // three segments is not a namespace either, and neither is an empty one
    [InlineData("io/file/extra")]
    [InlineData("io/")]
    [InlineData("/file")]
    [InlineData("io/file#viewer")]
    public void Parse_InvalidCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<NamespacePath>.Failure>(NamespacePath.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace_path.invalid", error.Code);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = NamespacePath.Unchecked("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Unchecked_DoesNotLowercase()
    {
        var id = NamespacePath.Unchecked("IO/DOC");
        Assert.Equal("IO/DOC", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = NamespacePath.Unchecked("io/doc");
        var b = NamespacePath.Unchecked("io/doc");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = NamespacePath.Unchecked("io/doc");
        var b = NamespacePath.Unchecked("io/user");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = NamespacePath.Unchecked("io/a");
        var b = NamespacePath.Unchecked("io/b");

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
        var id = NamespacePath.Unchecked("io/doc");
        Assert.Equal("io/doc", id.ToString());
    }
}
