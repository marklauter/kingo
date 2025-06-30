using Kingo.Facts;
using System.Text.Json.Serialization;

namespace Kingo.Configuration.Spec;

public sealed record NamespaceSpec(
    Namespace Name,
    IReadOnlyList<RelationshipSpec> Relationships);

public sealed record RelationshipSpec(
    Relationship Name,
    SubjectSetRewriteRule? SubjectSetRewrite);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(This), nameof(This))]
[JsonDerivedType(typeof(ComputedSubjectSet), nameof(ComputedSubjectSet))]
[JsonDerivedType(typeof(UnionRewriteRule), nameof(UnionRewriteRule))]
[JsonDerivedType(typeof(IntersectionRewriteRule), nameof(IntersectionRewriteRule))]
[JsonDerivedType(typeof(ExclusionRewriteRule), nameof(ExclusionRewriteRule))]
public abstract record SubjectSetRewriteRule;

public sealed record This
    : SubjectSetRewriteRule;

public sealed record ComputedSubjectSet(
    Relationship Relationship)
    : SubjectSetRewriteRule;

public sealed record UnionRewriteRule(
    IReadOnlyList<SubjectSetRewriteRule> Children)
    : SubjectSetRewriteRule;

public sealed record IntersectionRewriteRule(
    IReadOnlyList<SubjectSetRewriteRule> Children)
    : SubjectSetRewriteRule;

public sealed record ExclusionRewriteRule(
    SubjectSetRewriteRule Include,
    SubjectSetRewriteRule Exclude)
    : SubjectSetRewriteRule;
