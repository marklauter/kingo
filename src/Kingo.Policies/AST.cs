using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Policies;

public sealed record PdlDocument(
    string Pdl,
    NamespaceSet PolicySet);

// <policy-set>
public sealed record NamespaceSet(
    Seq<Namespace> Policies);

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "it's fine")]
// <namespace>
public sealed record Namespace(
    // <policy-identifier>
    NamespaceIdentifier Name,
    // <relation-set>
    Seq<Relation> Relations);

// <relation>
public sealed record Relation(
    // <identifier> from <relation-identifier>
    RelationIdentifier Name,
    // <rewrite>
    SubjectSetRewrite SubjectSetRewrite)
{
    public Relation(RelationIdentifier name)
        : this(name, DirectRewrite.Default) { }
};

// <rewrite>
public abstract record SubjectSetRewrite;

// <direct>
public sealed record DirectRewrite
    : SubjectSetRewrite
{
    public static DirectRewrite Default { get; } = new();
}

// <computed-subjectset-rewrite>
public sealed record ComputedSubjectSetRewrite(
    RelationIdentifier Relation)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetRewrite Cons(RelationIdentifier relationship) =>
        new(relationship);
}

// <tuple-to-subjectset-rewrite>
public sealed record TupleToSubjectSetRewrite(
    RelationIdentifier TuplesetRelation,
    RelationIdentifier ComputedSubjectSetRelation)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TupleToSubjectSetRewrite Cons(RelationIdentifier tuplesetRelation, RelationIdentifier computedSubjectSetRelation) =>
        new(tuplesetRelation, computedSubjectSetRelation);
}

public sealed record UnionRewrite(
    Seq<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionRewrite Cons(IEnumerable<SubjectSetRewrite> children) =>
        new(Seq.createRange(children));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionRewrite Cons(Seq<SubjectSetRewrite> children) =>
        new(children);
}

public sealed record IntersectionRewrite(
    Seq<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionRewrite Cons(IEnumerable<SubjectSetRewrite> children) =>
        new(Seq.createRange(children));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionRewrite Cons(Seq<SubjectSetRewrite> children) =>
        new(children);
}

public sealed record ExclusionRewrite(
    SubjectSetRewrite Include,
    SubjectSetRewrite Exclude)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExclusionRewrite Cons(SubjectSetRewrite include, SubjectSetRewrite exclude) =>
        new(include, exclude);
}
