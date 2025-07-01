using Kingo.Facts;
using System.Text.Json.Serialization;

namespace Kingo.Specs;

public sealed record NamespaceSpec(
    Namespace Name,
    IReadOnlyList<RelationshipSpec> Relationships);

public sealed record RelationshipSpec(
    Relationship Name,
    SubjectSetRewrite? SubjectSetRewrite);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(This), nameof(This))]
[JsonDerivedType(typeof(ComputedSubjectSetRewrite), nameof(ComputedSubjectSetRewrite))]
[JsonDerivedType(typeof(UnionRewrite), nameof(UnionRewrite))]
[JsonDerivedType(typeof(IntersectionRewrite), nameof(IntersectionRewrite))]
[JsonDerivedType(typeof(ExclusionRewrite), nameof(ExclusionRewrite))]
public abstract record SubjectSetRewrite;

public sealed record This
    : SubjectSetRewrite;

public sealed record ComputedSubjectSetRewrite(
    Relationship Relationship)
    : SubjectSetRewrite;

public sealed record UnionRewrite(
    IReadOnlyList<SubjectSetRewrite> Children)
    : SubjectSetRewrite;

public sealed record IntersectionRewrite(
    IReadOnlyList<SubjectSetRewrite> Children)
    : SubjectSetRewrite;

public sealed record ExclusionRewrite(
    SubjectSetRewrite Include,
    SubjectSetRewrite Exclude)
    : SubjectSetRewrite;
