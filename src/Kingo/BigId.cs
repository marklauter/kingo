using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo;

[JsonConverter(typeof(ULongConvertible<BigId>))]
public readonly struct BigId
    : IStringConvertible<BigId>
    , IULongConvertible<BigId>
    , IEquatable<BigId>
    , IComparable<BigId>
    , IEquatable<ulong>
    , IComparable<ulong>
{
    private readonly ulong value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BigId(ulong l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private BigId(string s)
        : this(Parse(s))
    {
    }

    public static BigId Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigId From(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigId From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Parse(string s) =>
        ulong.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(BigId other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BigId(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(BigId c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BigId(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(BigId c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is BigId clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigId Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(BigId other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ulong other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ulong other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(BigId left, BigId right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(BigId left, BigId right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(BigId left, BigId right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(BigId left, BigId right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(BigId left, BigId right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(BigId left, BigId right) => left.CompareTo(right) >= 0;
}
