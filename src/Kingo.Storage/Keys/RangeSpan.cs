namespace Kingo.Storage.Keys;

public sealed record Between<RK>(RK FromKey, RK ToKey)
    : RangeKey
    where RK : IEquatable<RK>, IComparable<RK>;
