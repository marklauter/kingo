namespace Kingo.Storage.Ranges;

public sealed record RangeSpan(string FromKey, string ToKey) : UnboundRange;
