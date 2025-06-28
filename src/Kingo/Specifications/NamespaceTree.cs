using Kingo.Facts;
using System.Runtime.CompilerServices;

namespace Kingo.Specifications;

public sealed record NamespaceTree(
    Namespace Name,
    IReadOnlyDictionary<Relationship, RelationshipTree> Relationships)
{
    public static NamespaceTree FromSpec(NamespaceSpec spec) =>
        new(spec.Name, spec.Relationships
            .ToDictionary(
                r => r.Name,
                r => new RelationshipTree(
                    r.Name,
                    ConvertRewrite(r.SubjectSetRewrite)
                )
            ));

    private static SubjectSetRewriteNode ConvertRewrite(SubjectSetRewriteRule? rule) =>
        rule is null
            ? ThisNode.This
            : rule switch
            {
                This => ThisNode.This,
                ComputedSubjectSet c => ComputedNode.From(c.Relationship),
                TupleToSubjectSet t => TupleToNode.From(t.Tupleset, ConvertRewrite(t.ComputedSetRewrite)),
                SubjectSetRewriteOperation o => OperationNode.From(
                    o.Operation,
                    o.Children.Select(ConvertRewrite).ToList()
                ),
                _ => throw new NotSupportedException()
            };
}

public sealed record RelationshipTree(
    Relationship Name,
    SubjectSetRewriteNode RewriteRoot);

public abstract record SubjectSetRewriteNode;

public sealed record ThisNode : SubjectSetRewriteNode
{
    public static ThisNode This { get; } = new ThisNode();
}

public sealed record ComputedNode(Relationship Relationship) : SubjectSetRewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedNode From(Relationship relationship) => new(relationship);
}

public sealed record TupleToNode(Relationship Tupleset, SubjectSetRewriteNode Child) : SubjectSetRewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TupleToNode From(Relationship tupleset, SubjectSetRewriteNode child) => new(tupleset, child);
}

public sealed record OperationNode(SetOperation Operation, IReadOnlyList<SubjectSetRewriteNode> Children) : SubjectSetRewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OperationNode From(SetOperation operation, IReadOnlyList<SubjectSetRewriteNode> children) => new(operation, children);
}
