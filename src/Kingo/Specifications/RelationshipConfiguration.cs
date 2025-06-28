using Kingo.Facts;

namespace Kingo.Specifications;

public sealed record RelationshipSpec(
    Relationship Name,
    SubjectSetRewriteRule? SubjectSetRewrite);
