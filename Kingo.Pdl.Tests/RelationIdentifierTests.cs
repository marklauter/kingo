namespace Kingo.Pdl.Tests;

public sealed class RelationIdentifierTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => RelationIdentifier.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => RelationIdentifier.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    [InlineData("a.b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>(nameof(value), () => RelationIdentifier.From(value));
        Assert.StartsWith("value contains invalid characters", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a_b")]
    [InlineData("a0")]
    [InlineData("...")]
    public void From_ReturnsIdentifier_IfValueIsValid(string value)
    {
        var relation = RelationIdentifier.From(value);
        Assert.Equal(value, relation.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var a = RelationIdentifier.From("a");
        var b = RelationIdentifier.From("a");

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var a = RelationIdentifier.From("a");
        var b = RelationIdentifier.From("b");

        Assert.False(a.Equals(b));
        Assert.False(a.Equals((object)b));
        Assert.False(a == b);
        Assert.True(a != b);
        Assert.False(a.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var a = RelationIdentifier.From("a");
        var b = RelationIdentifier.From("b");

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(a < b);
        Assert.True(a <= b);
        Assert.False(a > b);
        Assert.False(a >= b);
        Assert.True(a.CompareTo("b") < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualIdentifiers()
    {
        var a = RelationIdentifier.From("a");
        var b = RelationIdentifier.From("a");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var relation = RelationIdentifier.From(value);
        Assert.Equal(value, relation.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => RelationIdentifier.Empty());

    [Fact]
    public void Nothing_Returns_Three_Dots()
    {
        var nothing = RelationIdentifier.Nothing;
        Assert.Equal("...", nothing.ToString());
    }

    [Fact]
    public void Value_ReturnsUnderlyingString()
    {
        var id = RelationIdentifier.From("owner");
        Assert.Equal("owner", id.Value);
    }

    [Fact]
    public void Create_DoesNotValidate()
    {
        // Trusted path — caller asserts the value is valid; Create accepts what From would reject.
        var id = RelationIdentifier.Create("not-a-valid-identifier");
        Assert.Equal("not-a-valid-identifier", id.Value);
    }

    [Fact]
    public void Parse_ValidInput_ReturnsSuccess()
    {
        var result = RelationIdentifier.Parse("owner");
        var s = Assert.IsType<Success<RelationIdentifier>>(result);
        Assert.Equal("owner", s.Value.Value);
    }

    [Fact]
    public void Parse_NothingSentinel_ReturnsSuccess()
    {
        var result = RelationIdentifier.Parse("...");
        var s = Assert.IsType<Success<RelationIdentifier>>(result);
        Assert.Equal("...", s.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a-b")]
    [InlineData("a.b")]
    [InlineData("0bad")]
    public void Parse_InvalidInput_ReturnsValidationFailure(string input)
    {
        var result = RelationIdentifier.Parse(input);
        var f = Assert.IsType<Failure<RelationIdentifier>>(result);
        Assert.Equal(ErrorType.Validation, f.Error.Type);
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueAndPopulatesOut()
    {
        Assert.True(RelationIdentifier.TryParse("owner", out var parsed));
        Assert.Equal("owner", parsed.Value);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseAndOutIsDefault()
    {
        Assert.False(RelationIdentifier.TryParse("bad-id", out var parsed));
        Assert.Equal(default, parsed);
    }
}
