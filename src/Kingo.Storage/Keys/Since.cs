namespace Kingo.Storage.Keys;

public sealed record Since<RK>(RK FromKey)
    : RangeKey
    where RK : IEquatable<RK>, IComparable<RK>;
