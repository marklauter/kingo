using Results;

namespace Kingo.Tests;

public sealed class SubjectIdTests
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
    // the caller's shapes: a GUID, an integer, a URN, a URI, an email, and a UPN — a subject id is opaque to Kingo
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
        var s = Assert.IsType<Result<SubjectId>.Success>(SubjectId.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("Anne")]
    [InlineData("MixedCase")]
    public void Parse_PreservesCase(string input)
    {
        var s = Assert.IsType<Result<SubjectId>.Success>(SubjectId.Parse(input));
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
        // null reaches Parse only through reflection callers (see IParse); it lands in the empty guard
        var f = Assert.IsType<Result<SubjectId>.Failure>(SubjectId.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("subject_id.empty", error.Code);
    }

    [Theory]
    [InlineData("a b")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    public void Parse_WhitespaceOrControlCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<SubjectId>.Failure>(SubjectId.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("subject_id.invalid", error.Code);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = SubjectId.Unchecked("a#b@c");
        Assert.Equal("a#b@c", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = SubjectId.Unchecked("anne");
        var b = SubjectId.Unchecked("anne");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = SubjectId.Unchecked("anne");
        var b = SubjectId.Unchecked("bob");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_CaseSensitive_UppercaseBeforeLowercase()
    {
        var upper = SubjectId.Unchecked("A");
        var lower = SubjectId.Unchecked("a");

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
        var id = SubjectId.Unchecked("anne");
        Assert.Equal("anne", id.ToString());
    }
}
