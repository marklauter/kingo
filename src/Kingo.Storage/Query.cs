using System.Numerics;

namespace Kingo.Storage;

public record Query<D, HK>(
    HK HashKey,
    RangeKeyCondition? RangeKeyCondition = null,
    Func<D, bool>? Filter = null)
    where HK : IEquatable<HK>, IComparable<HK>;

public sealed record Query<D, HK, N>(
    HK HashKey,
    INumber<N> Version,
    RangeKeyCondition? RangeKeyCondition = null,
    Func<D, bool>? Filter = null)
    : Query<D, HK>(HashKey, RangeKeyCondition, Filter)
    where HK : IEquatable<HK>, IComparable<HK>
    where N : INumber<N>;
