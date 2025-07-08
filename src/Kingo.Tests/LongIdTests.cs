using System.Globalization;

namespace Kingo.Tests;

public sealed class BigIdTests
{
    [Fact]
    public void Zero_ReturnsDefaultBigId() =>
        Assert.Equal(default, BigId.Zero);

    [Fact]
    public void From_Long_ReturnsBigId()
    {
        const long value = 123;
        var id = BigId.From(value);
        Assert.Equal<BigId>(value, id);
    }

    [Theory]
    [InlineData("123", 123L)]
    [InlineData(" 123 ", 123L)]
    [InlineData("", 0L)]
    [InlineData(" ", 0L)]
    [InlineData(null, 0L)]
    public void From_String_ReturnsBigId(string? value, ulong expected)
    {
        var id = BigId.From(value!);
        Assert.Equal<BigId>(expected, id);
    }

    [Fact]
    public void From_String_Throws_IfValueIsNotANumber()
    {
        var exception = Assert.Throws<FormatException>(() => BigId.From("abc"));
        Assert.Equal("The input string 'abc' was not in a correct format.", exception.Message);
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var id1 = BigId.From(123);
        var id2 = BigId.From(123);

        Assert.True(id1.Equals(id2));
        Assert.True(id1.Equals((object)id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.True(id1.Equals(123UL));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var id1 = BigId.From(123);
        var id2 = BigId.From(456);

        Assert.False(id1.Equals(id2));
        Assert.False(id1.Equals((object)id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
        Assert.False(id1.Equals(456UL));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var id1 = BigId.From(123);
        var id2 = BigId.From(456);

        Assert.True(id1.CompareTo(id2) < 0);
        Assert.True(id1 < id2);
        Assert.True(id1 <= id2);
        Assert.False(id1 > id2);
        Assert.False(id1 >= id2);
        Assert.True(id1.CompareTo(456UL) < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualBigIds()
    {
        var id1 = BigId.From(123);
        var id2 = BigId.From(123);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const long value = 123;
        var id = BigId.From(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const long value = 123;
        var id = BigId.From(value);
        string s = id;
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), s);
    }

    [Fact]
    public void ImplicitConversion_ToBigId_FromString_ReturnsBigId()
    {
        const string value = "123";
        BigId id = value;
        Assert.Equal(BigId.From(123), id);
    }

    [Fact]
    public void ImplicitConversion_ToBigId_FromLong_ReturnsBigId()
    {
        const long value = 123;
        BigId id = value;
        Assert.Equal(BigId.From(value), id);
    }

    [Fact]
    public void Empty_ReturnsZero() =>
        Assert.Equal(BigId.Zero, BigId.Empty());
}
