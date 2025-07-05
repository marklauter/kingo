using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo.Storage.Clocks;

[JsonConverter(typeof(StringConvertible<Revision>))]
public readonly struct Revision
    : IStringConvertible<Revision>
    , IULongConvertible<Revision>
    , IEquatable<Revision>
    , IComparable<Revision>
    , IEquatable<ulong>
    , IComparable<ulong>
{
    private readonly ulong value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Revision(ulong l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private Revision(string s)
        : this(Parse(s))
    {
    }

    public static Revision Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Revision From(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Revision From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Revision Tick() => new(value + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Parse(string s) =>
        ulong.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Revision other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Revision(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Revision c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Revision(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(Revision c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Revision clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Revision Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Revision other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ulong other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ulong other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Revision left, Revision right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Revision left, Revision right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Revision left, Revision right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Revision left, Revision right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Revision left, Revision right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Revision left, Revision right) => left.CompareTo(right) >= 0;
}
