using System.Numerics;

namespace Kingo.Storage;

public record Query<D, HK>(
    HK HashKey,
    RangeKeyCondition? RangeKeyCondition,
    Func<D, bool>? Filter)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Query(HK HashKey, RangeKeyCondition? RangeKeyCondition)
        : this(HashKey, RangeKeyCondition, null) { }

    public Query(HK HashKey)
        : this(HashKey, null, null) { }
}

public record Query<D, HK, N>(
    HK HashKey,
    RangeKeyCondition? RangeKeyCondition,
    Func<D, bool>? Filter,
    INumber<N>? Version)
    where HK : IEquatable<HK>, IComparable<HK>
    where N : INumber<N>
{
    public Query(HK HashKey, RangeKeyCondition? RangeKeyCondition)
        : this(HashKey, RangeKeyCondition, null, null) { }

    public Query(HK HashKey)
        : this(HashKey, null, null, null) { }
}
