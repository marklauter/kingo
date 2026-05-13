namespace Kingo.Policies.Tests;

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
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var identifier = NamespaceIdentifier.From(value);
        string s = identifier;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_FromString_ReturnsIdentifier()
    {
        const string value = "a_b_c";
        NamespaceIdentifier identifier = value;
        Assert.Equal(value, identifier.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => NamespaceIdentifier.Empty());
}
