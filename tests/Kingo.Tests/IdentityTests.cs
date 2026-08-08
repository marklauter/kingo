using Results;

namespace Kingo.Tests;

public sealed class IdentityTests
{
    [Theory]
    [InlineData("anne")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    [InlineData("a.b")]
    [InlineData("a-b")]
    [InlineData("0abc")]
    [InlineData("a.")]
    [InlineData("a-")]
    [InlineData(".a")]
    [InlineData("-a")]
    [InlineData("café")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("42")]
    [InlineData("urn:isbn:0451450523")]
    [InlineData("https://example.com/a#b")]
    [InlineData("carol@example.com")]
    [InlineData("carol@corp.onmicrosoft.com")]
    [InlineData("user:anne")]
    [InlineData("a:b")]
    [InlineData("a#b")]
    [InlineData("a@b")]
    [InlineData("a/b")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<Identity>.Success>(Identity.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("Anne")]
    [InlineData("MixedCase")]
    public void Parse_PreservesCase(string input)
    {
        var s = Assert.IsType<Result<Identity>.Success>(Identity.Parse(input));
        Assert.Equal(input, s.Value.Value);
        Assert.Equal(input, s.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_NullEmptyOrWhitespace_ReturnsEmptyValidationFailure(string? input)
    {
        var f = Assert.IsType<Result<Identity>.Failure>(Identity.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("identity.empty", error.Code.Value);
    }

    [Theory]
    [InlineData("a b")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    public void Parse_WhitespaceOrControlCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<Identity>.Failure>(Identity.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("identity.invalid", error.Code.Value);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = Identity.Unchecked("a#b@c");
        Assert.Equal("a#b@c", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = Identity.Unchecked("anne");
        var b = Identity.Unchecked("anne");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = Identity.Unchecked("anne");
        var b = Identity.Unchecked("bob");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_CaseSensitive_UppercaseBeforeLowercase()
    {
        var upper = Identity.Unchecked("A");
        var lower = Identity.Unchecked("a");

        Assert.True(upper.CompareTo(lower) < 0);
        Assert.True(upper < lower);
        Assert.True(upper <= lower);
        Assert.False(upper > lower);
        Assert.False(upper >= lower);
    }

    [Fact]
    public void ToString_ReturnsRawValue()
    {
        var id = Identity.Unchecked("anne");
        Assert.Equal("anne", id.ToString());
    }
}
