namespace Kingo.Storage;

public abstract record RangeKeyCondition
{
    public static RangeKeyCondition IsEqualTo(object key) => new EqualsCondition(key);
    public static RangeKeyCondition IsGreaterThan(object key) => new GreaterThanCondition(key);
    public static RangeKeyCondition IsGreaterThanOrEqualTo(object key) => new GreaterThanOrEqualCondition(key);
    public static RangeKeyCondition IsLessThan(object key) => new LessThanCondition(key);
    public static RangeKeyCondition IsLessThanOrEqualTo(object key) => new LessThanOrEqualCondition(key);
    public static RangeKeyCondition IsBetweenExclusive(object lowerBound, object upperBound) => new BetweenExlusiveCondition(lowerBound, upperBound);
    public static RangeKeyCondition IsBetweenInclusive(object lowerBound, object upperBound) => new BetweenInclusiveCondition(lowerBound, upperBound);
}

public sealed record EqualsCondition(object Key)
    : RangeKeyCondition;

public sealed record GreaterThanCondition(object Key)
    : RangeKeyCondition;

public sealed record GreaterThanOrEqualCondition(object Key)
    : RangeKeyCondition;

public sealed record LessThanCondition(object Key)
    : RangeKeyCondition;

public sealed record LessThanOrEqualCondition(object Key)
    : RangeKeyCondition;

public sealed record BetweenExlusiveCondition(object LowerBound, object UpperBound)
    : RangeKeyCondition;

public sealed record BetweenInclusiveCondition(object LowerBound, object UpperBound)
    : RangeKeyCondition;

