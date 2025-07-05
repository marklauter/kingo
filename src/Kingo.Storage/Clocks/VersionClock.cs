using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo.Storage.Clocks;

[JsonConverter(typeof(StringConvertible<VersionClock>))]
public readonly struct VersionClock
    : IStringConvertible<VersionClock>
    , IULongConvertible<VersionClock>
    , IEquatable<VersionClock>
    , IComparable<VersionClock>
    , IEquatable<ulong>
    , IComparable<ulong>
{
    private readonly ulong value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VersionClock(ulong l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private VersionClock(string s)
        : this(Parse(s))
    {
    }

    public static VersionClock Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionClock From(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionClock From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VersionClock Tick() => new(value + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Parse(string s) =>
        ulong.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(VersionClock other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VersionClock(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(VersionClock c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VersionClock(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(VersionClock c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is VersionClock clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionClock Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(VersionClock other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ulong other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ulong other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(VersionClock left, VersionClock right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(VersionClock left, VersionClock right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(VersionClock left, VersionClock right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(VersionClock left, VersionClock right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(VersionClock left, VersionClock right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(VersionClock left, VersionClock right) => left.CompareTo(right) >= 0;
}
