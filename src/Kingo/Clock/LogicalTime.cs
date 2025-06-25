using Kingo.Json;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Kingo.Clock;

[JsonConverter(typeof(StringConvertible<LogicalClock>))]
public readonly struct LogicalClock
    : IStringConvertible<LogicalClock>
{
    public long Value { get; }

    private LogicalClock(long tick) => Value = NonNegative(tick);

    [JsonConstructor]
    private LogicalClock(string s)
        : this(Parse(s))
    {
    }

    public static LogicalClock Zero { get; }
    public static LogicalClock From(long tick) => new(tick);
    public static LogicalClock From(string s) => new(s);

    public LogicalClock Tick() => new(Value + 1);

    private static long NonNegative(long tick) => tick >= 0 ? tick : 0;

    private static long Parse(string s) =>
        long.Parse(string.IsNullOrWhiteSpace(s) ? "0" : s, NumberStyles.Number, CultureInfo.InvariantCulture);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    public override int GetHashCode() => Value.GetHashCode();

    public static implicit operator LogicalClock(string s) => new(s);
    public static implicit operator string(LogicalClock t) => t.ToString();
}
