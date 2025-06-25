using Kingo.Clock;

namespace Kingo.Storage;

public record Document(string HashKey, string RangeKey, LogicalClock Version, DateTime Timestamp)
{
    public static Document<T> New<T>(string hashKey, string rangeKey, LogicalClock version, T tuple) =>
        new(hashKey, rangeKey, version, DateTime.UtcNow, tuple);
}

public sealed record Document<T>(
    string HashKey,
    string RangeKey,
    LogicalClock Version,
    DateTime Timestamp,
    T Tuple)
    : Document(HashKey, RangeKey, Version, Timestamp)
    where T : notnull;
