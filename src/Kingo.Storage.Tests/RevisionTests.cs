using System.Globalization;
using System.Text.Json;

namespace Kingo.Storage.Tests;

public sealed class RevisionTests
{
    [Fact]
    public void Zero_ReturnsDefaultRevision() => Assert.Equal(default, Revision.Zero);

    [Fact]
    public void From_UInt64_ReturnsRevision()
    {
        const int value = 123;
        var clock = Revision.From(value);
        Assert.Equal<Revision>(value, clock);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData(" 123 ", 123)]
    [InlineData("", 0)]
    [InlineData(" ", 0)]
    [InlineData(null, 0)]
    public void From_String_ReturnsRevision(string? value, int expected)
    {
        var clock = Revision.From(value!);
        Assert.Equal<Revision>(expected, clock);
    }

    [Fact]
    public void Tick_IncrementsValue()
    {
        var clock = Revision.From(123);
        var ticked = clock.Tick();
        Assert.Equal<Revision>(124, ticked);
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var clock1 = Revision.From(123);
        var clock2 = Revision.From(123);

        Assert.True(clock1.Equals(clock2));
        Assert.True(clock1.Equals((object)clock2));
        Assert.True(clock1 == clock2);
        Assert.False(clock1 != clock2);
        Assert.True(clock1.Equals(123));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var clock1 = Revision.From(123);
        var clock2 = Revision.From(456);

        Assert.False(clock1.Equals(clock2));
        Assert.False(clock1.Equals((object)clock2));
        Assert.False(clock1 == clock2);
        Assert.True(clock1 != clock2);
        Assert.False(clock1.Equals(456));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var clock1 = Revision.From(123);
        var clock2 = Revision.From(456);

        Assert.True(clock1.CompareTo(clock2) < 0);
        Assert.True(clock1 < clock2);
        Assert.True(clock1 <= clock2);
        Assert.False(clock1 > clock2);
        Assert.False(clock1 >= clock2);
        Assert.True(clock1.CompareTo(456) < 0);
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue_ForUlong()
    {
        var clock = Revision.From(123);

        Assert.True(clock.CompareTo(456) < 0);
        Assert.Equal(0, clock.CompareTo(123));
        Assert.True(clock.CompareTo(10) > 0);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForNullAndOtherTypes()
    {
        var clock = Revision.From(123);
        Assert.False(clock.Equals(null!));
        Assert.False(clock.Equals(new object()));
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualClocks()
    {
        var clock1 = Revision.From(123);
        var clock2 = Revision.From(123);
        Assert.Equal(clock1.GetHashCode(), clock2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const int value = 123;
        var clock = Revision.From(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), clock.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const int value = 123;
        var clock = Revision.From(value);
        string s = clock;
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), s);
    }

    [Fact]
    public void ImplicitConversion_ToRevision_FromString_ReturnsRevision()
    {
        const string value = "123";
        Revision clock = value;
        Assert.Equal(Revision.From(123), clock);
    }

    [Fact]
    public void ImplicitConversion_ToRevision_FromULong_ReturnsRevision()
    {
        const int value = 123;
        Revision clock = value;
        Assert.Equal(Revision.From(value), clock);
    }

    [Fact]
    public void JsonSerialization_SerializesToString()
    {
        var clock = Revision.From(123);
        var json = JsonSerializer.Serialize(clock);
        Assert.Equal("\"123\"", json);
    }

    [Fact]
    public void JsonDeserialization_DeserializesFromString()
    {
        var json = "\"123\"";
        _ = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Revision>(json));
    }

    [Fact]
    public void Empty_ReturnsZero() => Assert.Equal(Revision.Zero, Revision.Empty());
}
