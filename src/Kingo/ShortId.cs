using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo;

[JsonConverter(typeof(IntConvertible<ShortId>))]
public readonly struct ShortId
    : IStringConvertible<ShortId>
    , IIntConvertible<ShortId>
    , IEquatable<ShortId>
    , IComparable<ShortId>
    , IEquatable<int>
    , IComparable<int>
{
    private readonly int value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ShortId(int l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private ShortId(string s)
        : this(Parse(s))
    {
    }

    public static ShortId Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShortId From(int l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShortId From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Parse(string s) =>
        int.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ShortId other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ShortId(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(ShortId c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ShortId(int l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ShortId c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is ShortId clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShortId Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ShortId other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(int other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(int other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ShortId left, ShortId right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ShortId left, ShortId right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(ShortId left, ShortId right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(ShortId left, ShortId right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(ShortId left, ShortId right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(ShortId left, ShortId right) => left.CompareTo(right) >= 0;
}

