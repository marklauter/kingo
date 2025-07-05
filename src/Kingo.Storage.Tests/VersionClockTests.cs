using Kingo.Storage.Clocks;
using System.Globalization;
using System.Text.Json;

namespace Kingo.Storage.Tests;

public sealed class VersionClockTests
{
    [Fact]
    public void Zero_ReturnsDefaultVersionClock() => Assert.Equal(default, VersionClock.Zero);

    [Fact]
    public void From_UInt64_ReturnsVersionClock()
    {
        const ulong value = 123;
        var clock = VersionClock.From(value);
        Assert.Equal<VersionClock>(value, clock);
    }

    [Theory]
    [InlineData("123", 123UL)]
    [InlineData(" 123 ", 123UL)]
    [InlineData("", 0UL)]
    [InlineData(" ", 0UL)]
    [InlineData(null, 0UL)]
    public void From_String_ReturnsVersionClock(string? value, ulong expected)
    {
        var clock = VersionClock.From(value!);
        Assert.Equal<VersionClock>(expected, clock);
    }

    [Fact]
    public void Tick_IncrementsValue()
    {
        var clock = VersionClock.From(123);
        var ticked = clock.Tick();
        Assert.Equal<VersionClock>(124UL, ticked);
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var clock1 = VersionClock.From(123);
        var clock2 = VersionClock.From(123);

        Assert.True(clock1.Equals(clock2));
        Assert.True(clock1.Equals((object)clock2));
        Assert.True(clock1 == clock2);
        Assert.False(clock1 != clock2);
        Assert.True(clock1.Equals(123UL));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var clock1 = VersionClock.From(123);
        var clock2 = VersionClock.From(456);

        Assert.False(clock1.Equals(clock2));
        Assert.False(clock1.Equals((object)clock2));
        Assert.False(clock1 == clock2);
        Assert.True(clock1 != clock2);
        Assert.False(clock1.Equals(456UL));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var clock1 = VersionClock.From(123);
        var clock2 = VersionClock.From(456);

        Assert.True(clock1.CompareTo(clock2) < 0);
        Assert.True(clock1 < clock2);
        Assert.True(clock1 <= clock2);
        Assert.False(clock1 > clock2);
        Assert.False(clock1 >= clock2);
        Assert.True(clock1.CompareTo(456UL) < 0);
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue_ForUlong()
    {
        var clock = VersionClock.From(123);

        Assert.True(clock.CompareTo(456UL) < 0);
        Assert.Equal(0, clock.CompareTo(123UL));
        Assert.True(clock.CompareTo(10UL) > 0);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForNullAndOtherTypes()
    {
        var clock = VersionClock.From(123);
        Assert.False(clock.Equals(null!));
        Assert.False(clock.Equals(new object()));
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualClocks()
    {
        var clock1 = VersionClock.From(123);
        var clock2 = VersionClock.From(123);
        Assert.Equal(clock1.GetHashCode(), clock2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const ulong value = 123;
        var clock = VersionClock.From(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), clock.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const ulong value = 123;
        var clock = VersionClock.From(value);
        string s = clock;
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), s);
    }

    [Fact]
    public void ImplicitConversion_ToVersionClock_FromString_ReturnsClock()
    {
        const string value = "123";
        VersionClock clock = value;
        Assert.Equal<VersionClock>(123UL, clock);
    }

    [Fact]
    public void ImplicitConversion_ToVersionClock_FromULong_ReturnsClock()
    {
        const ulong value = 123;
        VersionClock clock = value;
        Assert.Equal<VersionClock>(value, clock);
    }

    [Fact]
    public void JsonSerialization_SerializesToString()
    {
        var clock = VersionClock.From(123);
        var json = JsonSerializer.Serialize(clock);
        Assert.Equal("\"123\"", json);
    }

    [Fact]
    public void JsonDeserialization_DeserializesFromString()
    {
        var json = "\"123\"";
        var clock = JsonSerializer.Deserialize<VersionClock>(json);
        Assert.Equal<VersionClock>(123UL, clock);
    }

    [Fact]
    public void Empty_ReturnsZero() => Assert.Equal(VersionClock.Zero, VersionClock.Empty());
}
