using Kingo.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Kingo;

[JsonConverter(typeof(StringConvertible<LogicalClock>))]
public readonly struct LogicalClock
    : IStringConvertible<LogicalClock>
    , IEquatable<LogicalClock>
{
    public long Value { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LogicalClock(long tick) => Value = NonNegative(tick);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private LogicalClock(string s)
        : this(Parse(s))
    {
    }

    public static LogicalClock Zero { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogicalClock From(long tick) => new(tick);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogicalClock From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicalClock Tick() => new(Value + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long NonNegative(long tick) => tick >= 0 ? tick : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Parse(string s) =>
        long.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LogicalClock other) => Value == other.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LogicalClock(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(LogicalClock t) => t.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is LogicalClock clock && Equals(clock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogicalClock Empty() => Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LogicalClock left, LogicalClock right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LogicalClock left, LogicalClock right) => !(left == right);
}
