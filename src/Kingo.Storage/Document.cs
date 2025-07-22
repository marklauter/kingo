namespace Kingo.Storage;

public abstract record Document(
    Revision Version)
{
    public Document()
        : this(Revision.Zero)
    { }
};

public abstract record Document<HK>(
    HK HashKey,
    Revision Version)
    : Document(Version)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Document(HK hashKey)
        : this(hashKey, Revision.Zero)
    { }
}

public abstract record Document<HK, RK>(
    HK HashKey,
    RK RangeKey,
    Revision Version)
    : Document<HK>(HashKey, Version)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Document(HK hashKey, RK rangeKey)
        : this(hashKey, rangeKey, Revision.Zero)
    { }
}
