using Kingo.Configuration.Spec;
using Kingo.Facts;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Configuration.Tree;

internal sealed record NamespaceTree(
    Namespace Name,
    IReadOnlyDictionary<Relationship, RewriteNode> Relationships)
{
    public static NamespaceTree FromSpec(NamespaceSpec spec) =>
        new(spec.Name, spec.Relationships
            .ToDictionary(
                r => r.Name,
                r => ConvertRewrite(r.SubjectSetRewrite)
            ));

    private static RewriteNode ConvertRewrite(SubjectSetRewriteRule? rule) =>
        rule is null
            ? ThisNode.This
            : rule switch
            {
                This => ThisNode.This,
                ComputedSubjectSet c => ComputedSubjectSetNode.From(
                    c.Relationship),
                TupleToSubjectSet t => TupleToSubjectSetNode.From(
                    t.Name,
                    ConvertRewrite(t.ComputedSetRewrite)),
                SubjectSetRewriteOperation o => OperationNode.From(
                    o.Operation,
                    o.Children.Select(ConvertRewrite).ToArray()
                ),
                _ => throw new NotSupportedException()
            };
}

internal abstract record RewriteNode;

internal sealed record ThisNode : RewriteNode
{
    public static ThisNode This { get; } = new ThisNode();
}

internal sealed record ComputedSubjectSetNode(Relationship Relationship) : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetNode From(Relationship relationship) => new(relationship);
}

internal sealed record TupleToSubjectSetNode(Identifier Name, RewriteNode Child) : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TupleToSubjectSetNode From(Identifier name, RewriteNode child) => new(name, child);
}

internal sealed record OperationNode(SetOperation Operation, RewriteNode[] Children) : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OperationNode From(SetOperation operation, RewriteNode[] children) => new(operation, children);
}
