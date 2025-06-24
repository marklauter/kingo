using Kingo.Json;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Kingo.Clock;

[JsonConverter(typeof(StringConvertible<LogicalTime>))]
public readonly record struct LogicalTime
    : IStringConvertible<LogicalTime>
{
    public long Tick { get; }

    private LogicalTime(long tick) => Tick = GreaterThanZero(tick);

    [JsonConstructor]
    private LogicalTime(string s)
        : this(Parse(s))
    {
    }

    public static LogicalTime From(long tick) => new(tick);
    public static LogicalTime From(string s) => new(s);

    private static long GreaterThanZero(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tick, 0);
        return tick;
    }

    private static long Parse(string s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s);
        return long.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    public override string ToString() => Tick.ToString(CultureInfo.InvariantCulture);

    public static implicit operator LogicalTime(string s) => new(s);
    public static implicit operator string(LogicalTime t) => t.ToString();
}
