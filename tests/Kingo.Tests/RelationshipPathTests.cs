using Results;

namespace Kingo.Tests;

public sealed class RelationshipPathTests
{
    [Theory]
    [InlineData("owner")]
    [InlineData("user_group")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<RelationshipPath>.Success>(RelationshipPath.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("OWNER", "owner")]
    [InlineData("User_Group", "user_group")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<RelationshipPath>.Success>(RelationshipPath.Parse(input));
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
        var f = Assert.IsType<Result<RelationshipPath>.Failure>(RelationshipPath.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_path.empty", error.Code);
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
        var f = Assert.IsType<Result<RelationshipPath>.Failure>(RelationshipPath.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_path.invalid", error.Code);
    }

    [Fact]
    public void Parse_DotsMarker_IsRefused()
    {
        // '...' is not a relationship — it is the '#...' marker of the ResourceFact member production, fact-grammar
        // punctuation. The identifier grammar is name-only, so '...' fails Parse rather than naming a sentinel relationship.
        var f = Assert.IsType<Result<RelationshipPath>.Failure>(RelationshipPath.Parse("..."));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_path.invalid", error.Code);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("....")]
    [InlineData("a...")]
    [InlineData("...a")]
    public void Parse_PartialDots_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<RelationshipPath>.Failure>(RelationshipPath.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_path.invalid", error.Code);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = RelationshipPath.Unchecked("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Unchecked_DoesNotLowercase()
    {
        var id = RelationshipPath.Unchecked("OWNER");
        Assert.Equal("OWNER", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = RelationshipPath.Unchecked("owner");
        var b = RelationshipPath.Unchecked("owner");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = RelationshipPath.Unchecked("owner");
        var b = RelationshipPath.Unchecked("editor");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = RelationshipPath.Unchecked("a");
        var b = RelationshipPath.Unchecked("b");

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
        var id = RelationshipPath.Unchecked("owner");
        Assert.Equal("owner", id.ToString());
    }
}
