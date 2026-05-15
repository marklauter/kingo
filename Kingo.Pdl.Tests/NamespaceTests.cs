namespace Kingo.Pdl.Tests;

public sealed class NamespaceTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => NamespaceIdentifier.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => NamespaceIdentifier.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    [InlineData("a.b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>(nameof(value), () => NamespaceIdentifier.From(value));
        Assert.StartsWith("value contains invalid characters", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("_a")]
    [InlineData("a_b")]
    [InlineData("a0")]
    public void From_ReturnsIdentifier_IfValueIsValid(string value)
    {
        var identifier = NamespaceIdentifier.From(value);
        Assert.Equal(value, identifier.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var a = NamespaceIdentifier.From("a");
        var b = NamespaceIdentifier.From("a");

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var a = NamespaceIdentifier.From("a");
        var b = NamespaceIdentifier.From("b");

        Assert.False(a.Equals(b));
        Assert.False(a.Equals((object)b));
        Assert.False(a == b);
        Assert.True(a != b);
        Assert.False(a.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var a = NamespaceIdentifier.From("a");
        var b = NamespaceIdentifier.From("b");

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
        var a = NamespaceIdentifier.From("a");
        var b = NamespaceIdentifier.From("a");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var identifier = NamespaceIdentifier.From(value);
        Assert.Equal(value, identifier.ToString());
    }

    [Fact]
    public void Value_ReturnsUnderlyingString()
    {
        var id = NamespaceIdentifier.From("file");
        Assert.Equal("file", id.Value);
    }

    [Fact]
    public void Create_DoesNotValidate()
    {
        // Trusted path — caller asserts the value is valid; Create accepts what From would reject.
        var id = NamespaceIdentifier.Create("not-a-valid-identifier");
        Assert.Equal("not-a-valid-identifier", id.Value);
    }

    [Fact]
    public void Parse_ValidInput_ReturnsSuccess()
    {
        var result = NamespaceIdentifier.Parse("file");
        var s = Assert.IsType<Success<NamespaceIdentifier>>(result);
        Assert.Equal("file", s.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a-b")]
    [InlineData("a.b")]
    [InlineData("0bad")]
    public void Parse_InvalidInput_ReturnsValidationFailure(string input)
    {
        var result = NamespaceIdentifier.Parse(input);
        var f = Assert.IsType<Failure<NamespaceIdentifier>>(result);
        Assert.Equal(ErrorType.Validation, f.Error.Type);
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueAndPopulatesOut()
    {
        Assert.True(NamespaceIdentifier.TryParse("file", out var parsed));
        Assert.Equal("file", parsed.Value);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseAndOutIsDefault()
    {
        Assert.False(NamespaceIdentifier.TryParse("bad-id", out var parsed));
        Assert.Equal(default, parsed);
    }
}
