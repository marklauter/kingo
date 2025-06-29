﻿using Kingo.Facts;
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SetOperation
{
    Union,
    Intersection,
    Exclusion,
}
