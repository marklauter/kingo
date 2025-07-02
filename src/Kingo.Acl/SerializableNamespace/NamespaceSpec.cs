using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kingo.Acl.SerializableNamespace;

public sealed record NamespaceSpec(
    Namespace Name,
    IReadOnlyList<RelationshipSpec> Relationships)
{
    public static async Task<NamespaceSpec> FromFileAsync(string path) =>
        JsonSerializer.Deserialize<NamespaceSpec>(await File.ReadAllTextAsync(path))!;
}

public sealed record RelationshipSpec
{
    public RelationshipSpec(Relationship name, SubjectSetRewrite? subjectSetRewrite)
    {
        Name = name;
        SubjectSetRewrite = subjectSetRewrite ?? new This();
    }

    public Relationship Name { get; }
    public SubjectSetRewrite SubjectSetRewrite { get; }
};

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(This), nameof(This))]
[JsonDerivedType(typeof(ComputedSubjectSetRewrite), nameof(ComputedSubjectSetRewrite))]
[JsonDerivedType(typeof(UnionRewrite), nameof(UnionRewrite))]
[JsonDerivedType(typeof(IntersectionRewrite), nameof(IntersectionRewrite))]
[JsonDerivedType(typeof(ExclusionRewrite), nameof(ExclusionRewrite))]
[JsonDerivedType(typeof(TupleToSubjectSetRewrite), nameof(TupleToSubjectSetRewrite))]
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

public sealed record TupleToSubjectSetRewrite(
    Relationship TuplesetRelation,
    Relationship ComputedSubjectSetRelation)
    : SubjectSetRewrite;

