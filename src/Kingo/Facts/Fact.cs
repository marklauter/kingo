using Kingo.Clock;

namespace Kingo.Facts;

public abstract record Fact(string Id, LogicalTime Version)
{
    public override string ToString() => Id;
}

