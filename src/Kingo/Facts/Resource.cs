using Kingo.Clock;

namespace Kingo.Facts;

public sealed record Resource(
    string Id,
    LogicalTime Version,
    Namespace Namespace)
    : Fact(Id, Version)
    , IKey<string>
{
    public override string AsKey() => $"{Namespace}:{Id}";

    public override string ToString() => AsKey();
}

