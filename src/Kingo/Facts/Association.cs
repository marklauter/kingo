using Kingo.Clock;
using LanguageExt;

namespace Kingo.Facts;

public record Association(
    string Id,
    LogicalTime Version,
    Resource Resource,
    Relationship Relationship,
    Either<Subject, SubjectSet> Subject)
    : Fact(Id, Version)
{
    public override string ToString() => $"{Resource}#{Relationship}@{Subject}";
}

