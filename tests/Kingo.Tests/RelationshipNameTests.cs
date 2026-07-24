using Results;

namespace Kingo.Tests;

public sealed class RelationshipNameTests
{
    [Theory]
    [InlineData("owner")]
    [InlineData("user_group")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<RelationshipName>.Success>(RelationshipName.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("OWNER", "owner")]
    [InlineData("User_Group", "user_group")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<RelationshipName>.Success>(RelationshipName.Parse(input));
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
        var f = Assert.IsType<Result<RelationshipName>.Failure>(RelationshipName.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_name.empty", error.Code);
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
    // a name is one segment: a qualified path is not one
    [InlineData("io/file#viewer")]
    [InlineData("io/file")]
    [InlineData("file#viewer")]
    public void Parse_InvalidCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<RelationshipName>.Failure>(RelationshipName.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_name.invalid", error.Code);
    }

    [Theory]
    [InlineData("...")] // the '#...' marker of the ResourceFact member production is punctuation, not a name
    [InlineData("..")]
    [InlineData("....")]
    [InlineData("a...")]
    [InlineData("...a")]
    public void Parse_DotsMarkerAndPartialDots_AreRefused(string input)
    {
        var f = Assert.IsType<Result<RelationshipName>.Failure>(RelationshipName.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("relationship_name.invalid", error.Code);
    }

    [Fact]
    public void Parse_AcceptsTheSameGrammarAsTheOtherNames()
    {
        // a spec name, a namespace name, and a relationship name are the same production — one segment
        // ([[identifiers]]); this pins that the three do not drift apart silently. 'this' is included
        // deliberately: the core accepts it as a name, and reserving it is the SDL parser's job, not this type's
        string[] inputs = ["viewer", "VIEWER", "_x", "a1", "this", "This", "0abc", "a-b", "a.b", "a b", "", "io/file"];

        foreach (var input in inputs)
        {
            Assert.Equal(
                RelationshipName.Parse(input) is Result<RelationshipName>.Success,
                SpecName.Parse(input) is Result<SpecName>.Success);
            Assert.Equal(
                RelationshipName.Parse(input) is Result<RelationshipName>.Success,
                NamespaceName.Parse(input) is Result<NamespaceName>.Success);
        }
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = RelationshipName.Unchecked("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Unchecked_DoesNotLowercase()
    {
        var id = RelationshipName.Unchecked("OWNER");
        Assert.Equal("OWNER", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = RelationshipName.Unchecked("owner");
        var b = RelationshipName.Unchecked("owner");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = RelationshipName.Unchecked("owner");
        var b = RelationshipName.Unchecked("editor");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = RelationshipName.Unchecked("a");
        var b = RelationshipName.Unchecked("b");

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
        var id = RelationshipName.Unchecked("owner");
        Assert.Equal("owner", id.ToString());
    }
}
