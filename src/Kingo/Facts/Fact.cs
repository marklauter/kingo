using Kingo.Clock;

namespace Kingo.Facts;

public abstract record Fact
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

    public override string ToString() => Id;
}

