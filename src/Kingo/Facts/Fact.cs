using Kingo.Clock;

namespace Kingo.Facts;

public abstract record Fact
    : IKey<string>
{
    public Fact(string id, LogicalTime version)
    {
        Id = Normalize(id);
        Version = version;
    }

    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToLowerInvariant();
    }

    public string Id { get; }
    public LogicalTime Version { get; }

    public virtual string AsKey() => Id;

    public override string ToString() => AsKey();
}

