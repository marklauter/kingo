using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage;

public record Document(
    Revision Version,
    Map<Key, string> Data)
{
    public static Document<HK> Cons<HK>(HK hashKey, Map<Key, string> Data)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new(hashKey, Data);

    public static Document<HK> Cons<HK>(HK hashKey, Revision version, Map<Key, string> Data)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new(hashKey, version, Data);

    public static Document<HK, RK> Cons<HK, RK>(HK hashKey, RK rangeKey, Map<Key, string> Data)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new(hashKey, rangeKey, Data);

    public static Document<HK, RK> Cons<HK, RK>(HK hashKey, RK rangeKey, Revision version, Map<Key, string> Data)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new(hashKey, rangeKey, version, Data);
}

public record Document<HK>(
    HK HashKey,
    Revision Version,
    Map<Key, string> Data)
    : Document(Version, Data)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Document(
        HK hashKey,
        Map<Key, string> data)
        : this(hashKey, Revision.Zero, data)
    {
    }
}

public record Document<HK, RK>(
    HK HashKey,
    RK RangeKey,
    Revision Version,
    Map<Key, string> Data)
    : Document<HK>(HashKey, Version, Data)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Document(
        HK hashKey,
        RK rangeKey,
        Map<Key, string> data)
        : this(hashKey, rangeKey, Revision.Zero, data)
    {
    }
}
