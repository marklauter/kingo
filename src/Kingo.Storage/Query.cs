using LanguageExt;

namespace Kingo.Storage;

public record Query<D, HK>(
    HK HashKey,
    Option<RangeKeyCondition> RangeKeyCondition,
    Option<Func<D, bool>> Filter)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Query(HK HashKey, Option<RangeKeyCondition> RangeKeyCondition)
        : this(HashKey, RangeKeyCondition, Prelude.None) { }
}
