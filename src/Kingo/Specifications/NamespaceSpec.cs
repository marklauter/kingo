using Kingo.Facts;
using System.Text.Json.Serialization;

namespace Kingo.Specifications;

public sealed record NamespaceSpec(
    Namespace Name,
    IReadOnlyList<RelationshipSpec> Relationships);

public sealed record RelationshipSpec(
    Relationship Name,
    SubjectSetRewriteRule? SubjectSetRewrite);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(This), nameof(This))]
[JsonDerivedType(typeof(ComputedSubjectSet), nameof(ComputedSubjectSet))]
[JsonDerivedType(typeof(TupleToSubjectSet), nameof(TupleToSubjectSet))]
[JsonDerivedType(typeof(SubjectSetRewriteOperation), nameof(SubjectSetRewriteOperation))]
public abstract record SubjectSetRewriteRule;

public sealed record This
    : SubjectSetRewriteRule;

public sealed record ComputedSubjectSet(
    Relationship Relationship)
    : SubjectSetRewriteRule;

public sealed record SubjectSetRewriteOperation(
    SetOperation Operation,
    IReadOnlyList<SubjectSetRewriteRule> Children)
    : SubjectSetRewriteRule;

public sealed record TupleToSubjectSet(
    Identifier Name,
    SubjectSetRewriteRule ComputedSetRewrite)
    : SubjectSetRewriteRule;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SetOperation
{
    Union,
    Intersection,
    Exclusion,
}
