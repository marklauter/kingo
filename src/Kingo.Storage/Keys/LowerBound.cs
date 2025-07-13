namespace Kingo.Storage.Keys;

public sealed record LowerBound<RK>(RK Key)
    : RangeKey
    where RK : IEquatable<RK>, IComparable<RK>;
