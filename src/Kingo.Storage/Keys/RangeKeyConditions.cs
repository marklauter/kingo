namespace Kingo.Storage.Keys;

public abstract record RangeKeyCondition
{
    public static RangeKeyCondition IsEqualTo<RK>(RK key) where RK : IEquatable<RK>, IComparable<RK> => new EqualsCondition<RK>(key);
    public static RangeKeyCondition IsGreaterThan<RK>(RK key) where RK : IEquatable<RK>, IComparable<RK> => new GreaterThanCondition<RK>(key);
    public static RangeKeyCondition IsGreaterThanOrEqualTo<RK>(RK key) where RK : IEquatable<RK>, IComparable<RK> => new GreaterThanOrEqualCondition<RK>(key);
    public static RangeKeyCondition IsLessThan<RK>(RK key) where RK : IEquatable<RK>, IComparable<RK> => new LessThanCondition<RK>(key);
    public static RangeKeyCondition IsLessThanOrEqualTo<RK>(RK key) where RK : IEquatable<RK>, IComparable<RK> => new LessThanOrEqualCondition<RK>(key);
    public static RangeKeyCondition IsBetweenExclusive<RK>(RK lowerBound, RK upperBound) where RK : IEquatable<RK>, IComparable<RK> => new BetweenExlusiveCondition<RK>(lowerBound, upperBound);
    public static RangeKeyCondition IsBetweenInclusive<RK>(RK lowerBound, RK upperBound) where RK : IEquatable<RK>, IComparable<RK> => new BetweenInclusiveCondition<RK>(lowerBound, upperBound);
}

public sealed record EqualsCondition<RK>(RK Key)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record GreaterThanCondition<RK>(RK Key)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record GreaterThanOrEqualCondition<RK>(RK Key)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record LessThanCondition<RK>(RK Key)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record LessThanOrEqualCondition<RK>(RK Key)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record BetweenExlusiveCondition<RK>(RK LowerBound, RK UpperBound)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

public sealed record BetweenInclusiveCondition<RK>(RK LowerBound, RK UpperBound)
    : RangeKeyCondition
    where RK : IEquatable<RK>, IComparable<RK>;

