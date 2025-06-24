using Kingo.Clock;

namespace Kingo.Facts;

public sealed record Resource(
    string Id,
    LogicalTime Version,
    Namespace Namespace)
    : Fact(Id, Version)
{
    public override string ToString() => $"{Namespace}:{Id}";
}

