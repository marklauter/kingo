using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Policies;

public sealed record PdlDocument(
    string Yaml,
    ImmutableArray<Namespace> Namespaces)
{
    public bool Equals(PdlDocument? other) =>
        other is not null
        && Yaml == other.Yaml
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Yaml, PolicyHash.OfSequence(Namespaces));
}

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "the domain word is 'namespace'")]
public sealed record Namespace(
    NamespaceIdentifier Name,
    ImmutableArray<Relation> Relations)
{
    public bool Equals(Namespace? other) =>
        other is not null
        && Name.Equals(other.Name)
        && Relations.AsSpan().SequenceEqual(other.Relations.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, PolicyHash.OfSequence(Relations));
}

public sealed record Relation(
    RelationIdentifier Name,
    SubjectSetRewrite SubjectSetRewrite)
{
    public Relation(RelationIdentifier name)
        : this(name, ThisRewrite.Default) { }
}

public abstract record SubjectSetRewrite;

public sealed record ThisRewrite : SubjectSetRewrite
{
    public static ThisRewrite Default { get; } = new();
}

public sealed record ComputedSubjectSetRewrite(
    RelationIdentifier Relation)
    : SubjectSetRewrite;

public sealed record TupleToSubjectSetRewrite(
    RelationIdentifier TuplesetRelation,
    RelationIdentifier ComputedSubjectSetRelation)
    : SubjectSetRewrite;

public sealed record UnionRewrite(
    ImmutableArray<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    public bool Equals(UnionRewrite? other) =>
        other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

    public override int GetHashCode() => PolicyHash.OfSequence(Children);
}

public sealed record IntersectionRewrite(
    ImmutableArray<SubjectSetRewrite> Children)
    : SubjectSetRewrite
{
    public bool Equals(IntersectionRewrite? other) =>
        other is not null && Children.AsSpan().SequenceEqual(other.Children.AsSpan());

    public override int GetHashCode() => PolicyHash.OfSequence(Children);
}

public sealed record ExclusionRewrite(
    SubjectSetRewrite Include,
    SubjectSetRewrite Exclude)
    : SubjectSetRewrite;

internal static class PolicyHash
{
    public static int OfSequence<T>(ImmutableArray<T> items)
    {
        var hash = new HashCode();
        foreach (var item in items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
