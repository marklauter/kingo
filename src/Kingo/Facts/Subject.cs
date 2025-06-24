using Kingo.Clock;

namespace Kingo.Facts;

public sealed record Subject(string Id, LogicalTime Version) : Fact(Id, Version);

