namespace Kingo.Tests;

public sealed class LongIdTests
{
    [Fact]
    public void Zero_ReturnsDefaultLongId() =>
        Assert.Equal(default, LongId.Zero);

    [Fact]
    public void From_Long_ReturnsLongId()
    {
        const long value = 123;
        var id = LongId.From(value);
        Assert.Equal<LongId>(value, id);
    }

    [Theory]
    [InlineData("123", 123L)]
    [InlineData(" 123 ", 123L)]
    [InlineData("", 0L)]
    [InlineData(" ", 0L)]
    [InlineData(null, 0L)]
    public void From_String_ReturnsLongId(string? value, long expected)
    {
        var id = LongId.From(value!);
        Assert.Equal<LongId>(expected, id);
    }

    [Fact]
    public void From_String_Throws_IfValueIsNotANumber()
    {
        var exception = Assert.Throws<FormatException>(() => LongId.From("abc"));
        Assert.Equal("The input string 'abc' was not in a correct format.", exception.Message);
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var id1 = LongId.From(123);
        var id2 = LongId.From(123);

        Assert.True(id1.Equals(id2));
        Assert.True(id1.Equals((object)id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.True(id1.Equals(123L));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var id1 = LongId.From(123);
        var id2 = LongId.From(456);

        Assert.False(id1.Equals(id2));
        Assert.False(id1.Equals((object)id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
        Assert.False(id1.Equals(456L));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var id1 = LongId.From(123);
        var id2 = LongId.From(456);

        Assert.True(id1.CompareTo(id2) < 0);
        Assert.True(id1 < id2);
        Assert.True(id1 <= id2);
        Assert.False(id1 > id2);
        Assert.False(id1 >= id2);
        Assert.True(id1.CompareTo(456L) < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualLongIds()
    {
        var id1 = LongId.From(123);
        var id2 = LongId.From(123);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const long value = 123;
        var id = LongId.From(value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const long value = 123;
        var id = LongId.From(value);
        string s = id;
        Assert.Equal(value.ToString(), s);
    }

    [Fact]
    public void ImplicitConversion_ToLongId_FromString_ReturnsLongId()
    {
        const string value = "123";
        LongId id = value;
        Assert.Equal(LongId.From(123), id);
    }

    [Fact]
    public void ImplicitConversion_ToLongId_FromLong_ReturnsLongId()
    {
        const long value = 123;
        LongId id = value;
        Assert.Equal(LongId.From(value), id);
    }

    [Fact]
    public void Empty_ReturnsZero() =>
        Assert.Equal(LongId.Zero, LongId.Empty());
}
