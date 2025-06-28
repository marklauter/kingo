using Kingo.Time;

namespace Kingo.Storage;

public record Document(
    Key HashKey,
    Key RangeKey,
    LogicalClock Version,
    DateTime Timestamp)
{
    public static Document<T> Cons<T>(Key hashKey, Key rangeKey, T tuple) where T : notnull =>
        new(hashKey, rangeKey, LogicalClock.Zero, DateTime.UtcNow, tuple);

    public static Document<T> Cons<T>(Key hashKey, Key rangeKey, LogicalClock version, T tuple) where T : notnull =>
        new(hashKey, rangeKey, version, DateTime.UtcNow, tuple);
}

public sealed record Document<T>(
    Key HashKey,
    Key RangeKey,
    LogicalClock Version,
    DateTime Timestamp,
    T Tuple)
    : Document(HashKey, RangeKey, Version, Timestamp)
    where T : notnull;
