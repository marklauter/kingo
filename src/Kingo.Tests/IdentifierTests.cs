namespace Kingo.Tests;

public sealed class IdentifierTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => Identifier.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => Identifier.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>("value", () => Identifier.From(value));
        Assert.StartsWith($"value contains invalid characters: '{value}'", exception.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a.b")]
    [InlineData("a_b")]
    [InlineData("a0")]
    public void From_ReturnsIdentifier_IfValueIsValid(string value)
    {
        var identifier = Identifier.From(value);
        Assert.Equal(value, identifier.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var identifier1 = Identifier.From("a");
        var identifier2 = Identifier.From("a");

        Assert.True(identifier1.Equals(identifier2));
        Assert.True(identifier1.Equals((object)identifier2));
        Assert.True(identifier1 == identifier2);
        Assert.False(identifier1 != identifier2);
        Assert.True(identifier1.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var identifier1 = Identifier.From("a");
        var identifier2 = Identifier.From("b");

        Assert.False(identifier1.Equals(identifier2));
        Assert.False(identifier1.Equals((object)identifier2));
        Assert.False(identifier1 == identifier2);
        Assert.True(identifier1 != identifier2);
        Assert.False(identifier1.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var identifier1 = Identifier.From("a");
        var identifier2 = Identifier.From("b");

        Assert.True(identifier1.CompareTo(identifier2) < 0);
        Assert.True(identifier1 < identifier2);
        Assert.True(identifier1 <= identifier2);
        Assert.False(identifier1 > identifier2);
        Assert.False(identifier1 >= identifier2);
        Assert.True(identifier1.CompareTo("b") < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualIdentifiers()
    {
        var identifier1 = Identifier.From("a");
        var identifier2 = Identifier.From("a");
        Assert.Equal(identifier1.GetHashCode(), identifier2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a.b_c";
        var identifier = Identifier.From(value);
        Assert.Equal(value, identifier.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a.b_c";
        var identifier = Identifier.From(value);
        string s = identifier;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_ToIdentifier_ReturnsIdentifier()
    {
        const string value = "a.b_c";
        Identifier identifier = value;
        Assert.Equal(value, identifier.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => Identifier.Empty());
}
