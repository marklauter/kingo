namespace Kingo.Storage;

public record Query<D, HK>(
    HK HashKey,
    RangeKeyCondition? RangeKeyCondition,
    Func<D, bool>? Filter)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Query(HK HashKey, RangeKeyCondition? RangeKeyCondition)
        : this(HashKey, RangeKeyCondition, null) { }
}
