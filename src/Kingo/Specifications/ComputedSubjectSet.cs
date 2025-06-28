using Kingo.Facts;

namespace Kingo.Specifications;

public sealed record ComputedSubjectSet(
    Relationship Relationship)
    : SubjectSetRewriteRule;
