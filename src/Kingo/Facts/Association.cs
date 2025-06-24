using LanguageExt;

namespace Kingo.Facts;

public readonly record struct Association(
    SubjectSet ResourceRelationship,
    Either<Subject, SubjectSet> Subject)
{
    public override string ToString() => $"{ResourceRelationship}@{Subject}";

    public static Association Cons(
        Resource resource,
        Relationship relationship,
        Either<Subject, SubjectSet> Subject) =>
        new(new(resource, relationship), Subject);
}

