namespace Kingo.Storage.Keys;

public abstract record RangeKey
{
    public static Unbound Unbound { get; } = new();

    public static LowerBound<RK> Since<RK>(RK key)
        where RK : IEquatable<RK>, IComparable<RK>
        => new(key);

    public static UpperBound<RK> Until<RK>(RK key)
        where RK : IEquatable<RK>, IComparable<RK>
        => new(key);

    public static Between<RK> Between<RK>(RK lowerBound, RK upperBound)
        where RK : IEquatable<RK>, IComparable<RK>
        => new(lowerBound, upperBound);
}
