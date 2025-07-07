namespace Kingo.Tests;

public sealed class ShortIdTests
{
    [Fact]
    public void Zero_ReturnsDefaultShortId() =>
        Assert.Equal(default, ShortId.Zero);

    [Fact]
    public void From_Int_ReturnsShortId()
    {
        const int value = 123;
        var id = ShortId.From(value);
        Assert.Equal<ShortId>(value, id);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData(" 123 ", 123)]
    [InlineData("", 0)]
    [InlineData(" ", 0)]
    [InlineData(null, 0)]
    public void From_String_ReturnsShortId(string? value, int expected)
    {
        var id = ShortId.From(value!);
        Assert.Equal<ShortId>(expected, id);
    }

    [Fact]
    public void From_String_Throws_IfValueIsNotANumber()
    {
        var exception = Assert.Throws<FormatException>(() => ShortId.From("abc"));
        Assert.Equal("The input string 'abc' was not in a correct format.", exception.Message);
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var id1 = ShortId.From(123);
        var id2 = ShortId.From(123);

        Assert.True(id1.Equals(id2));
        Assert.True(id1.Equals((object)id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.True(id1.Equals(123));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var id1 = ShortId.From(123);
        var id2 = ShortId.From(456);

        Assert.False(id1.Equals(id2));
        Assert.False(id1.Equals((object)id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
        Assert.False(id1.Equals(456));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var id1 = ShortId.From(123);
        var id2 = ShortId.From(456);

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
        var id1 = ShortId.From(123);
        var id2 = ShortId.From(123);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const int value = 123;
        var id = ShortId.From(value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const int value = 123;
        var id = ShortId.From(value);
        string s = id;
        Assert.Equal(value.ToString(), s);
    }

    [Fact]
    public void ImplicitConversion_ToShortId_FromString_ReturnsShortId()
    {
        const string value = "123";
        ShortId id = value;
        Assert.Equal(ShortId.From(123), id);
    }

    [Fact]
    public void ImplicitConversion_ToShortId_FromInt_ReturnsShortId()
    {
        const int value = 123;
        ShortId id = value;
        Assert.Equal(ShortId.From(value), id);
    }

    [Fact]
    public void Empty_ReturnsZero() =>
        Assert.Equal(ShortId.Zero, ShortId.Empty());
}
