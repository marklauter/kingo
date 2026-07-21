using Results;

namespace Kingo.Tests;

public sealed class ResourceIdentifierTests
{
    [Theory]
    [InlineData("doc")]
    [InlineData("readme")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    [InlineData("readme.md")]
    [InlineData("a-b")]
    [InlineData("0abc")]
    [InlineData("a.")]
    [InlineData("a-")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<ResourceIdentifier>.Success>(ResourceIdentifier.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("ReadMe.MD")]
    [InlineData("DOC")]
    [InlineData("MixedCase")]
    public void Parse_PreservesCase(string input)
    {
        var s = Assert.IsType<Result<ResourceIdentifier>.Success>(ResourceIdentifier.Parse(input));
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
        var f = Assert.IsType<Result<ResourceIdentifier>.Failure>(ResourceIdentifier.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("resource_id.empty", error.Code);
    }

    [Theory]
    [InlineData(".a")]
    [InlineData("-a")]
    [InlineData("a b")]
    [InlineData("café")]
    [InlineData("#")]
    [InlineData("@")]
    [InlineData("a:b")]
    [InlineData("a#b")]
    [InlineData("a@b")]
    public void Parse_InvalidCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<ResourceIdentifier>.Failure>(ResourceIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("resource_id.invalid", error.Code);
    }

    [Fact]
    public void Create_BypassesValidation_AcceptsRejectedInput()
    {
        var id = ResourceIdentifier.Create("a:b#c@d");
        Assert.Equal("a:b#c@d", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = ResourceIdentifier.Create("readme.md");
        var b = ResourceIdentifier.Create("readme.md");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = ResourceIdentifier.Create("readme.md");
        var b = ResourceIdentifier.Create("license.txt");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_CaseSensitive_UppercaseBeforeLowercase()
    {
        var upper = ResourceIdentifier.Create("A");
        var lower = ResourceIdentifier.Create("a");

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
        var id = ResourceIdentifier.Create("readme.md");
        Assert.Equal("readme.md", id.ToString());
    }
}
