using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Policies;

public sealed record PdlDocument(
    string Yaml,
    Seq<Namespace> Namespaces);

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "it's fine")]
public sealed record Namespace(
    NamespaceIdentifier Name,
    Seq<Relation> Relations);

public sealed record Relation(
    RelationIdentifier Name,
    SubjectSetRewrite SubjectSetRewrite)
{
    public Relation(RelationIdentifier name)
        : this(name, ThisRewrite.Default) { }
};

public abstract record SubjectSetRewrite;

public sealed record ThisRewrite
    : SubjectSetRewrite
{
    public static ThisRewrite Default { get; } = new();
}

public sealed record ComputedSubjectSetRewrite(
    RelationIdentifier Relation)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetRewrite Cons(RelationIdentifier relationship) =>
        new(relationship);
}

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
