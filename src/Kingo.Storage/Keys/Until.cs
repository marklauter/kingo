namespace Kingo.Storage.Keys;

public sealed record Until<RK>(RK ToKey)
    : RangeKey
    where RK : IEquatable<RK>, IComparable<RK>;

