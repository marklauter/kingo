using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo;

[JsonConverter(typeof(IntConvertible<SmallId>))]
public readonly struct SmallId
    : IStringConvertible<SmallId>
    , IIntConvertible<SmallId>
    , IEquatable<SmallId>
    , IComparable<SmallId>
    , IEquatable<int>
    , IComparable<int>
{
    private readonly int value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SmallId(int l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private SmallId(string s)
        : this(Parse(s))
    {
    }

    public static SmallId Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SmallId From(int l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SmallId From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Parse(string s) =>
        int.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(SmallId other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SmallId(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(SmallId c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SmallId(int l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(SmallId c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is SmallId clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SmallId Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SmallId other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(int other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(int other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(SmallId left, SmallId right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(SmallId left, SmallId right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(SmallId left, SmallId right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(SmallId left, SmallId right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(SmallId left, SmallId right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(SmallId left, SmallId right) => left.CompareTo(right) >= 0;
}

