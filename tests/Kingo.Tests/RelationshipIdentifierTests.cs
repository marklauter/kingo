using Results;

namespace Kingo.Tests;

public sealed class RelationshipIdentifierTests
{
    [Theory]
    [InlineData("owner")]
    [InlineData("user_group")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<RelationshipIdentifier>.Success>(RelationshipIdentifier.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("OWNER", "owner")]
    [InlineData("User_Group", "user_group")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<RelationshipIdentifier>.Success>(RelationshipIdentifier.Parse(input));
        Assert.Equal(expected, s.Value.Value);
        Assert.Equal(expected, s.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_EmptyOrWhitespace_ReturnsEmptyValidationFailure(string input)
    {
        var f = Assert.IsType<Result<RelationshipIdentifier>.Failure>(RelationshipIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_id.empty", error.Code);
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
        var f = Assert.IsType<Result<RelationshipIdentifier>.Failure>(RelationshipIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_id.invalid", error.Code);
    }

    [Fact]
    public void Nothing_Value_IsThreeDots() => Assert.Equal("...", RelationshipIdentifier.Nothing.Value);

    [Fact]
    public void Parse_NothingSentinel_SucceedsAndEqualsNothing()
    {
        var s = Assert.IsType<Result<RelationshipIdentifier>.Success>(RelationshipIdentifier.Parse("..."));
        Assert.Equal("...", s.Value.Value);
        Assert.Equal(RelationshipIdentifier.Nothing, s.Value);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("....")]
    [InlineData("a...")]
    [InlineData("...a")]
    public void Parse_PartialDots_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<RelationshipIdentifier>.Failure>(RelationshipIdentifier.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_id.invalid", error.Code);
    }

    [Fact]
    public void Create_BypassesValidation_AcceptsRejectedInput()
    {
        var id = RelationshipIdentifier.Create("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Create_DoesNotLowercase()
    {
        var id = RelationshipIdentifier.Create("OWNER");
        Assert.Equal("OWNER", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = RelationshipIdentifier.Create("owner");
        var b = RelationshipIdentifier.Create("owner");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = RelationshipIdentifier.Create("owner");
        var b = RelationshipIdentifier.Create("editor");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = RelationshipIdentifier.Create("a");
        var b = RelationshipIdentifier.Create("b");

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
        var id = RelationshipIdentifier.Create("owner");
        Assert.Equal("owner", id.ToString());
    }
}
