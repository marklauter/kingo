using Kingo.Storage.Clocks;

namespace Kingo.Storage;

public static class Document
{
    public static Document<HK, RK, R> Cons<HK, RK, R>(HK hashKey, RK rangeKey, R record)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
    where R : notnull =>
        new(hashKey, rangeKey, Revision.Zero, DateTime.UtcNow, record);

    public static Document<HK, RK, R> Cons<HK, RK, R>(HK hashKey, RK rangeKey, Revision version, R record)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
    where R : notnull =>
        new(hashKey, rangeKey, version, DateTime.UtcNow, record);
}

public record Document<HK, RK>(
    HK HashKey,
    RK RangeKey,
    Revision Version,
    DateTime Timestamp)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record Document<HK, RK, R>(
    HK HashKey,
    RK RangeKey,
    Revision Version,
    DateTime Timestamp,
    R Record) : Document<HK, RK>(HashKey, RangeKey, Version, Timestamp)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
    where R : notnull;
