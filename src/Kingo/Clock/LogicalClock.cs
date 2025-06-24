using Kingo.Json;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Kingo.Clock;

[JsonConverter(typeof(StringConvertible<LogicalClock>))]
public sealed class LogicalClock
    : IStringConvertible<LogicalClock>
{
    private long tick;

    public LogicalClock()
        : this(0)
    { }

    private LogicalClock(long seed) => tick = GreaterThanZero(seed);

    [JsonConstructor]
    private LogicalClock(string s)
        : this(Parse(s))
    {
    }

    /// <summary>
    /// Advances the logical time by incrementing the current tick value.
    /// </summary>
    /// <returns>A <see cref="LogicalTime"/> instance representing the updated logical time after the tick increment.</returns>
    public LogicalTime Tick() => LogicalTime.From(Interlocked.Increment(ref tick));

    public static LogicalClock From(long seed) => new(seed);
    public static LogicalClock From(string s) => new(s);
    public static LogicalClock Default { get; } = new LogicalClock();

    private static long GreaterThanZero(long seed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seed, 0);
        return seed;
    }

    private static long Parse(string s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s);
        return long.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    public override string ToString() => tick.ToString(CultureInfo.InvariantCulture);

    public static implicit operator LogicalClock(string s) => new(s);
    public static implicit operator string(LogicalClock t) => t.ToString();
};
