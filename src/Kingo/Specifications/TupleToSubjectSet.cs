using Kingo.Facts;

namespace Kingo.Specifications;

public sealed record TupleToSubjectSet(
    Relationship Tupleset,
    SubjectSetRewriteRule ComputedSetRewrite)
    : SubjectSetRewriteRule;
