namespace Kingo.Storage.Ranges;

public abstract record KeyRange
{
    public static Unbound Unbound() => new();
    public static Since Since(Key rangeKey) => new(rangeKey);
    public static Until Until(Key rangeKey) => new(rangeKey);
    public static Between Between(Key fromKey, Key toKey) => new(fromKey, toKey);
}
