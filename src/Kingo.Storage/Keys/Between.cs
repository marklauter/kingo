namespace Kingo.Storage.Keys;

public sealed record Between<RK>(RK LowerBound, RK UpperBound)
    : RangeKey
    where RK : IEquatable<RK>, IComparable<RK>;
