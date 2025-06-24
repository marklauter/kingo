using Kingo.Clock;

namespace Kingo.Facts;

public record SubjectSetRewrite(
    string Id,
    LogicalTime Version,
    Resource Resource,
    Relationship Relationship,
    SubjectSet SubjectSet)
    : Fact(Id, Version);
