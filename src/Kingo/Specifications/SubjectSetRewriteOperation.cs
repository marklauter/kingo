namespace Kingo.Specifications;

public sealed record SubjectSetRewriteOperation(
    SetOperation Operation,
    IReadOnlyList<SubjectSetRewriteRule> Children)
    : SubjectSetRewriteRule;
