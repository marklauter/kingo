using Kingo.Facts;
using System.Text.Json.Serialization;

namespace Kingo.Configuration.Spec;

internal sealed record NamespaceSpec(
    Namespace Name,
    IReadOnlyList<RelationshipSpec> Relationships);

internal sealed record RelationshipSpec(
    Relationship Name,
    SubjectSetRewriteRule? SubjectSetRewrite);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(This), nameof(This))]
[JsonDerivedType(typeof(ComputedSubjectSet), nameof(ComputedSubjectSet))]
[JsonDerivedType(typeof(TupleToSubjectSet), nameof(TupleToSubjectSet))]
[JsonDerivedType(typeof(SubjectSetRewriteOperation), nameof(SubjectSetRewriteOperation))]
internal abstract record SubjectSetRewriteRule;

internal sealed record This
    : SubjectSetRewriteRule;

internal sealed record ComputedSubjectSet(
    Relationship Relationship)
    : SubjectSetRewriteRule;

internal sealed record SubjectSetRewriteOperation(
    SetOperation Operation,
    IReadOnlyList<SubjectSetRewriteRule> Children)
    : SubjectSetRewriteRule;

internal sealed record TupleToSubjectSet(
    Identifier Name,
    SubjectSetRewriteRule ComputedSetRewrite)
    : SubjectSetRewriteRule;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum SetOperation
{
    Union,
    Intersection,
    Exclusion,
}
