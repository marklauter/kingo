using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo;

[JsonConverter(typeof(LongConvertible<LongId>))]
public readonly struct LongId
    : IStringConvertible<LongId>
    , ILongConvertible<LongId>
    , IEquatable<LongId>
    , IComparable<LongId>
    , IEquatable<long>
    , IComparable<long>
{
    private readonly long value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LongId(long l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private LongId(string s)
        : this(Parse(s))
    {
    }

    public static LongId Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LongId From(long l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LongId From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Parse(string s) =>
        long.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LongId other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LongId(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(LongId c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LongId(long l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator long(LongId c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is LongId clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LongId Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(LongId other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(long other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(long other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LongId left, LongId right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LongId left, LongId right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(LongId left, LongId right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(LongId left, LongId right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(LongId left, LongId right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(LongId left, LongId right) => left.CompareTo(right) >= 0;
}

