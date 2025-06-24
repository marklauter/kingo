namespace Kingo.Facts;

public sealed record SubjectSet(
    Resource Resource,
    Relationship Relationship)
    : IKey<string>
{
    public string AsKey() => $"{Resource}#{Relationship}";

    public override string ToString() => AsKey();
}

