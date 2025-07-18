using System.Globalization;

namespace Kingo.Tests;

//public readonly struct SnowflakeId(ulong value)
//{
//    private static class SDBMHash
//    {
//        // http://www.partow.net/programming/hashfunctions/index.html#GeneralHashFunctionLicense
//        public static uint ComputeHash(string value)
//        {
//            unchecked
//            {
//                uint hash = 0;
//                if (string.IsNullOrEmpty(value))
//                    return hash;

//                for (var i = value.Length - 1; i > -1; --i)
//                {
//                    hash = value[i] + (hash << 6) + (hash << 16) - hash;
//                }

//                return hash;
//            }
//        }
//    }

//    private readonly ulong value = value;

//    private const int TimestampBits = 42;
//    private const int MachineIdBits = 10;
//    private const int SequenceBits = 12;

//    private const uint MaxMachineId = (1U << MachineIdBits) - 1;
//    private const ulong MaxSequence = (1UL << SequenceBits) - 1;
//    private const ulong MaxTimestamp = (1UL << TimestampBits) - 1;

//    // Custom epoch: 2024-01-01 00:00:00 UTC
//    private static readonly ulong Epoch = (ulong)new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks;

//    public static SnowflakeId Generate(ulong sequenceValue) =>
//        new(PackSnowflakeId(
//            ((ulong)DateTimeOffset.UtcNow.Ticks - Epoch) / TimeSpan.TicksPerMillisecond,
//            SDBMHash.ComputeHash(Environment.MachineName) & MaxMachineId,
//            sequenceValue & MaxSequence));

//    private static ulong PackSnowflakeId(ulong timestamp, uint machineId, ulong sequence) =>
//        (timestamp << (MachineIdBits + SequenceBits)) |
//        ((ulong)machineId << SequenceBits) |
//        sequence;

//    public static implicit operator ulong(SnowflakeId id) => id.value;
//    public static implicit operator SnowflakeId(ulong value) => new() { value = value };

//    public override string ToString() => value.ToString();
//}

//public sealed class MachineIdTests
//{
//    public static uint GetMachineId()
//    {
//        var os = Environment.OSVersion.ToString();
//        var processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown";
//        var machineName = Environment.MachineName;

//        var rawId = $"{os}-{processor}-{machineName}";
//        var reversed = new string(rawId.Reverse().ToArray());

//        // SDBM is specifically good for your use case
//        return SDBMHash.ComputeHash(reversed) & SnowflakeId.MaxMachineId;
//    }

//    [Fact]
//    public void Test()
//    {
//        var x = GetMachineId();
//        var y = GetMachineId();
//        Assert.Equal(x, y);
//    }
//}

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
