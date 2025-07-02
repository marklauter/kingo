namespace Kingo.Tests;

public sealed class NamespaceTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => Namespace.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => Namespace.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>("value", () => Namespace.From(value));
        Assert.StartsWith($"value contains invalid characters: '{value}'", exception.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a.b")]
    [InlineData("a_b")]
    [InlineData("a0")]
    public void From_ReturnsNamespace_IfValueIsValid(string value)
    {
        var @namespace = Namespace.From(value);
        Assert.Equal(value, @namespace.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var namespace1 = Namespace.From("a");
        var namespace2 = Namespace.From("a");

        Assert.True(namespace1.Equals(namespace2));
        Assert.True(namespace1.Equals((object)namespace2));
        Assert.True(namespace1 == namespace2);
        Assert.False(namespace1 != namespace2);
        Assert.True(namespace1.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var namespace1 = Namespace.From("a");
        var namespace2 = Namespace.From("b");

        Assert.False(namespace1.Equals(namespace2));
        Assert.False(namespace1.Equals((object)namespace2));
        Assert.False(namespace1 == namespace2);
        Assert.True(namespace1 != namespace2);
        Assert.False(namespace1.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var namespace1 = Namespace.From("a");
        var namespace2 = Namespace.From("b");

        Assert.True(namespace1.CompareTo(namespace2) < 0);
        Assert.True(namespace1 < namespace2);
        Assert.True(namespace1 <= namespace2);
        Assert.False(namespace1 > namespace2);
        Assert.False(namespace1 >= namespace2);
        Assert.True(namespace1.CompareTo("b") < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualNamespaces()
    {
        var namespace1 = Namespace.From("a");
        var namespace2 = Namespace.From("a");
        Assert.Equal(namespace1.GetHashCode(), namespace2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a.b_c";
        var @namespace = Namespace.From(value);
        Assert.Equal(value, @namespace.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a.b_c";
        var @namespace = Namespace.From(value);
        string s = @namespace;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_ToNamespace_ReturnsNamespace()
    {
        const string value = "a.b_c";
        Namespace @namespace = value;
        Assert.Equal(value, @namespace.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => Namespace.Empty());
}
