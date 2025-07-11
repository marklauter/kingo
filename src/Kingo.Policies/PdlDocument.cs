using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Policies;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "it's a stupid rule")]
public interface PdlNode;

public sealed record PdlDocument(
    string Pdl,
    PolicySet PolicySet);

// <policy-set>
public sealed record PolicySet(
    Seq<Policy> Policies)
    : PdlNode;

// <policy>
public sealed record Policy(
    // <policy-identifier>
    PolicyName Name,
    // <relation-set>
    Seq<Relation> Relations)
    : PdlNode;

// <relation>
public sealed record Relation(
    // <identifier> from <relation-identifier>
    RelationName Name,
    // <rewrite>
    SubjectSetRewrite SubjectSetRewrite)
    : PdlNode
{
    public Relation(RelationName name)
        : this(name, DirectRewrite.Default) { }
};

// <rewrite>
public abstract record SubjectSetRewrite
    : PdlNode;

// <direct>
public sealed record DirectRewrite
    : SubjectSetRewrite
{
    public static DirectRewrite Default { get; } = new();
}

// <computed-subjectset-rewrite>
public sealed record ComputedSubjectSetRewrite(
    RelationName Relationship)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetRewrite Cons(RelationName relationship) =>
        new(relationship);
}

// <tuple-to-subjectset-rewrite>
public sealed record TupleToSubjectSetRewrite(
    RelationName TuplesetRelation,
    RelationName ComputedSubjectSetRelation)
    : SubjectSetRewrite
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TupleToSubjectSetRewrite Cons(RelationName tuplesetRelation, RelationName computedSubjectSetRelation) =>
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

