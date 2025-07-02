using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests.Keys;

public sealed class KeyTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => Key.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => Key.From(value));

    [Theory]
    [InlineData("a)b")]
    [InlineData("a(b")]
    [InlineData("(ab)")]
    [InlineData("a$b")]
    [InlineData("a b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>("value", () => Key.From(value));
        Assert.StartsWith($"value contains invalid characters: '{value}'", exception.Message);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("A", "a")]
    [InlineData("a.b", "a.b")]
    [InlineData("a_b", "a_b")]
    [InlineData("a0", "a0")]
    [InlineData("a:b", "a:b")]
    [InlineData("a/b", "a/b")]
    [InlineData("a@b", "a@b")]
    [InlineData("a#b", "a#b")]
    public void From_ReturnsKey_IfValueIsValid(string value, string expected)
    {
        var key = Key.From(value);
        Assert.Equal(expected, key.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var key1 = Key.From("a");
        var key2 = Key.From("A");

        Assert.True(key1.Equals(key2));
        Assert.True(key1.Equals((object)key2));
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
        Assert.True(key1.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var key1 = Key.From("a");
        var key2 = Key.From("b");

        Assert.False(key1.Equals(key2));
        Assert.False(key1.Equals((object)key2));
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
        Assert.False(key1.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var key1 = Key.From("a");
        var key2 = Key.From("b");

        Assert.True(key1.CompareTo(key2) < 0);
        Assert.True(key1 < key2);
        Assert.True(key1 <= key2);
        Assert.False(key1 > key2);
        Assert.False(key1 >= key2);
        Assert.True(key1.CompareTo("b") < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualKeys()
    {
        var key1 = Key.From("a");
        var key2 = Key.From("A");
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a.b_c:/@#";
        var key = Key.From(value);
        Assert.Equal(value, key.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a.b_c:/@#";
        var key = Key.From(value);
        string s = key;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_ToKey_ReturnsKey()
    {
        const string value = "a.b_c:/@#";
        Key key = value;
        Assert.Equal(value, key.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => Key.Empty());
}
