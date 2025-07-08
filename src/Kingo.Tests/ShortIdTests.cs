using System.Globalization;

namespace Kingo.Tests;

public sealed class SmallIdTests
{
    [Fact]
    public void Zero_ReturnsDefaultShortId() =>
        Assert.Equal(default, SmallId.Zero);

    [Fact]
    public void From_Int_ReturnsShortId()
    {
        const int value = 123;
        var id = SmallId.From(value);
        Assert.Equal<SmallId>(value, id);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData(" 123 ", 123)]
    [InlineData("", 0)]
    [InlineData(" ", 0)]
    [InlineData(null, 0)]
    public void From_String_ReturnsShortId(string? value, int expected)
    {
        var id = SmallId.From(value!);
        Assert.Equal<SmallId>(expected, id);
    }

    [Fact]
    public void From_String_Throws_IfValueIsNotANumber()
    {
        var exception = Assert.Throws<FormatException>(() => SmallId.From("abc"));
        Assert.Equal("The input string 'abc' was not in a correct format.", exception.Message);
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var id1 = SmallId.From(123);
        var id2 = SmallId.From(123);

        Assert.True(id1.Equals(id2));
        Assert.True(id1.Equals((object)id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.True(id1.Equals(123));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var id1 = SmallId.From(123);
        var id2 = SmallId.From(456);

        Assert.False(id1.Equals(id2));
        Assert.False(id1.Equals((object)id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
        Assert.False(id1.Equals(456));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var id1 = SmallId.From(123);
        var id2 = SmallId.From(456);

        Assert.True(id1.CompareTo(id2) < 0);
        Assert.True(id1 < id2);
        Assert.True(id1 <= id2);
        Assert.False(id1 > id2);
        Assert.False(id1 >= id2);
        Assert.True(id1.CompareTo(456) < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualShortIds()
    {
        var id1 = SmallId.From(123);
        var id2 = SmallId.From(123);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const int value = 123;
        var id = SmallId.From(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const int value = 123;
        var id = SmallId.From(value);
        string s = id;
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), s);
    }

    [Fact]
    public void ImplicitConversion_ToShortId_FromString_ReturnsShortId()
    {
        const string value = "123";
        SmallId id = value;
        Assert.Equal(SmallId.From(123), id);
    }

    [Fact]
    public void ImplicitConversion_ToShortId_FromInt_ReturnsShortId()
    {
        const int value = 123;
        SmallId id = value;
        Assert.Equal(SmallId.From(value), id);
    }

    [Fact]
    public void Empty_ReturnsZero() =>
        Assert.Equal(SmallId.Zero, SmallId.Empty());
}
