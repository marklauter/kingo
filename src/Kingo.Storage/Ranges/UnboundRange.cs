namespace Kingo.Storage.Ranges;

public record UnboundRange
{
    public static UnboundRange Unbound() => new();
    public static RangeSince Since(string rangeKey) => new(rangeKey);
    public static RangeUntil Until(string rangeKey) => new(rangeKey);
    public static RangeSpan Span(string fromKey, string toKey) => new(fromKey, toKey);
}
