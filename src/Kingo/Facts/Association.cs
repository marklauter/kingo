using LanguageExt;

namespace Kingo.Facts;

public readonly record struct Association(
    Resource Resource,
    Relationship Relationship,
    Either<Subject, SubjectSet> Subject)
{
    public override string ToString() => $"{Resource}#{Relationship}@{Subject}";
}

