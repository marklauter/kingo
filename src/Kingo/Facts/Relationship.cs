using Kingo.Clock;

namespace Kingo.Facts;

public sealed record Relationship(string Id, LogicalTime Version) : Fact(Id, Version);

