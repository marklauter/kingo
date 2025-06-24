using Kingo.Clock;

namespace Kingo.Facts;

public sealed record SubjectSet(
    string Id,
    LogicalTime Version,
    Resource Resource,
    Relationship Edge)
    : Fact(Id, Version)
{
    public override string ToString() => $"{Resource}#{Edge}";
}

