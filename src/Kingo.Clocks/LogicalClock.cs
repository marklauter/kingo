using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo.Clocks;

[JsonConverter(typeof(StringConvertible<LogicalClock>))]
public readonly struct LogicalClock
    : IStringConvertible<LogicalClock>
    , IULongConvertible<LogicalClock>
    , IEquatable<LogicalClock>
    , IComparable<LogicalClock>
    , IEquatable<ulong>
    , IComparable<ulong>
{
    private readonly ulong value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LogicalClock(ulong l) => value = l;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private LogicalClock(string s)
        : this(Parse(s))
    {
    }

    public static LogicalClock Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogicalClock From(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogicalClock From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicalClock Tick() => new(value + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Parse(string s) =>
        ulong.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LogicalClock other) => value == other.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LogicalClock(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(LogicalClock c) => c.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LogicalClock(ulong l) => new(l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(LogicalClock c) => c.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is LogicalClock clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogicalClock Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(LogicalClock other) => value.CompareTo(other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ulong other) => value == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ulong other) => value.CompareTo(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LogicalClock left, LogicalClock right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LogicalClock left, LogicalClock right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(LogicalClock left, LogicalClock right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(LogicalClock left, LogicalClock right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(LogicalClock left, LogicalClock right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(LogicalClock left, LogicalClock right) => left.CompareTo(right) >= 0;
}
