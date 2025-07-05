using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;

namespace Kingo.Storage;

public record Document(
    Key HashKey,
    Key RangeKey,
    Revision Version,
    DateTime Timestamp)
{
    public static Document<R> Cons<R>(Key hashKey, Key rangeKey, R record) where R : notnull =>
        new(hashKey, rangeKey, Revision.Zero, DateTime.UtcNow, record);

    public static Document<R> Cons<R>(Key hashKey, Key rangeKey, Revision version, R record) where R : notnull =>
        new(hashKey, rangeKey, version, DateTime.UtcNow, record);

    internal static Key FullHashKey<R>(Key hashKey) where R : notnull =>
        $"{TypeName<R>.Value}/{hashKey}";
}

public sealed record Document<R>(
    Key HashKey,
    Key RangeKey,
    Revision Version,
    DateTime Timestamp,
    R Record)
    : Document(HashKey, RangeKey, Version, Timestamp)
    where R : notnull
{
    internal Key FullHashKey => $"{TypeName<R>.Value}/{HashKey}";
}
