namespace Kingo.Storage.Keys;

public abstract record RangeKey
{
    public static Unbound Unbound { get; } = new();

    public static Since<RK> Since<RK>(RK rangeKey) where RK : IEquatable<RK>, IComparable<RK>
        => new(rangeKey);

    public static Until<RK> Until<RK>(RK rangeKey) where RK : IEquatable<RK>, IComparable<RK>
        => new(rangeKey);

    public static Between<RK> Between<RK>(RK fromKey, RK toKey) where RK : IEquatable<RK>, IComparable<RK>
        => new(fromKey, toKey);
}
