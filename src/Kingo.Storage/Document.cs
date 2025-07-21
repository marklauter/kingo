using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage;

public record Document(
    Revision Version,
    Map<Key, object> Data)
{
    public static Map<Key, object> ConsData(Key key, object value) => Map.create((key, value));

    public Option<object> this[Key key] => Data.Find(key);

    public Option<T> Field<T>(Key key) => Data.Find(key).Map(o => (T)o);

    public static Document<HK> Cons<HK>(HK hashKey, Map<Key, object> Data)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new(hashKey, Data);

    public static Document<HK> Cons<HK>(HK hashKey, Revision version, Map<Key, object> Data)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new(hashKey, version, Data);

    public static Document<HK, RK> Cons<HK, RK>(HK hashKey, RK rangeKey, Map<Key, object> Data)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new(hashKey, rangeKey, Data);

    public static Document<HK, RK> Cons<HK, RK>(HK hashKey, RK rangeKey, Revision version, Map<Key, object> Data)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new(hashKey, rangeKey, version, Data);
}

public record Document<HK>(
    HK HashKey,
    Revision Version,
    Map<Key, object> Data)
    : Document(Version, Data)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Document(
        HK hashKey,
        Map<Key, object> data)
        : this(hashKey, Revision.Zero, data)
    {
    }

    public Document(
        HK hashKey,
        Revision version,
        string data)
    : this(hashKey, version, MapSerializer.Deserialize(data))
    {
    }
}

public sealed record Document<HK, RK>(
    HK HashKey,
    RK RangeKey,
    Revision Version,
    Map<Key, object> Data)
    : Document<HK>(HashKey, Version, Data)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Document(
        HK hashKey,
        RK rangeKey,
        Map<Key, object> data)
        : this(hashKey, rangeKey, Revision.Zero, data)
    {
    }
}
