using Results;

namespace Kingo.Tests;

public sealed class SubjectIdentifierTests
{
    [Theory]
    [InlineData("anne")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    [InlineData("user:anne")]
    [InlineData("a.b")]
    [InlineData("a-b")]
    [InlineData("0abc")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<SubjectIdentifier>.Success>(SubjectIdentifier.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("User:Anne")]
    [InlineData("MixedCase")]
    public void Parse_PreservesCase(string input)
    {
        var s = Assert.IsType<Result<SubjectIdentifier>.Success>(SubjectIdentifier.Parse(input));
        Assert.Equal(input, s.Value.Value);
        Assert.Equal(input, s.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_EmptyOrWhitespace_ReturnsEmptyValidationFailure(string input)
    {
        var f = Assert.IsType<Result<SubjectIdentifier>.Failure>(SubjectIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("subject_id.empty", error.Code);
    }

    [Theory]
    [InlineData(":anne")]
    [InlineData(".a")]
    [InlineData("-a")]
    [InlineData("a b")]
    [InlineData("café")]
    [InlineData("a#b")]
    [InlineData("a@b")]
    [InlineData("#")]
    [InlineData("@")]
    public void Parse_InvalidCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<SubjectIdentifier>.Failure>(SubjectIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("subject_id.invalid", error.Code);
    }

    [Fact]
    public void Create_BypassesValidation_AcceptsRejectedInput()
    {
        var id = SubjectIdentifier.Create("a#b@c");
        Assert.Equal("a#b@c", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = SubjectIdentifier.Create("user:anne");
        var b = SubjectIdentifier.Create("user:anne");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = SubjectIdentifier.Create("user:anne");
        var b = SubjectIdentifier.Create("user:bob");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_CaseSensitive_UppercaseBeforeLowercase()
    {
        var upper = SubjectIdentifier.Create("A");
        var lower = SubjectIdentifier.Create("a");

        // ordinal ordering: 'A' (0x41) < 'a' (0x61)
        Assert.True(upper.CompareTo(lower) < 0);
        Assert.True(upper < lower);
        Assert.True(upper <= lower);
        Assert.False(upper > lower);
        Assert.False(upper >= lower);
    }

    [Fact]
    public void ToString_ReturnsRawValue()
    {
        var id = SubjectIdentifier.Create("user:anne");
        Assert.Equal("user:anne", id.ToString());
    }
}
