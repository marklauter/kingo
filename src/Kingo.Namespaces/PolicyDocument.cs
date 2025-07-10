using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Policies;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "it's a stupid rule")]
public interface PdlNode;

public sealed record Document(Seq<Policy> Policies)
    : PdlNode;

public sealed record Policy(
    PolicyName Name,
    Seq<Relationship> Relationships)
    : PdlNode;

public sealed record Relationship(RelationshipName Name, SubjectSetRewrite SubjectSetRewrite)
    : PdlNode
{
    public Relationship(RelationshipName name)
        : this(name, This.Default) { }
};

public abstract record SubjectSetRewrite
    : PdlNode;

public sealed record This
    : SubjectSetRewrite
{
    public static This Default { get; } = new();
}

public sealed record ComputedSubjectSetRewrite(
    RelationshipName Relationship)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetRewrite Cons(RelationshipName relationship) =>
        new(relationship);
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

public sealed record TupleToSubjectSetRewrite(
    RelationshipName TuplesetRelation,
    RelationshipName ComputedSubjectSetRelation)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TupleToSubjectSetRewrite Cons(RelationshipName tuplesetRelation, RelationshipName computedSubjectSetRelation) =>
        new(tuplesetRelation, computedSubjectSetRelation);
}
