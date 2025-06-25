using Kingo.Clock;

namespace Kingo.Storage;

public sealed record Document<T>(string HashKey, string RangeKey, LogicalTime Version, T Tuple)
    : Document(HashKey, RangeKey, Version);
