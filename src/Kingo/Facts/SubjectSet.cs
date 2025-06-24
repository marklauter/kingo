namespace Kingo.Facts;

public sealed record SubjectSet(
    Resource Resource,
    Relationship Relationship)
{
    public override string ToString() => $"{Resource}#{Relationship}";
}

