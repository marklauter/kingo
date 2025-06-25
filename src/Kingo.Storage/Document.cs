using Kingo.Clock;

namespace Kingo.Storage;

public record Document(string HashKey, string RangeKey, LogicalTime Version);
