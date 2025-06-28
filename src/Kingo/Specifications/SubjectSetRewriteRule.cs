using System.Text.Json.Serialization;

namespace Kingo.Specifications;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(This), nameof(This))]
[JsonDerivedType(typeof(ComputedSubjectSet), nameof(ComputedSubjectSet))]
[JsonDerivedType(typeof(TupleToSubjectSet), nameof(TupleToSubjectSet))]
[JsonDerivedType(typeof(SubjectSetRewriteOperation), nameof(SubjectSetRewriteOperation))]
public abstract record SubjectSetRewriteRule;
