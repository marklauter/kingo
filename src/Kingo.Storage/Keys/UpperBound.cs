namespace Kingo.Storage.Keys;

public sealed record UpperBound<RK>(RK Key)
    : RangeKey
    where RK : IEquatable<RK>, IComparable<RK>;

