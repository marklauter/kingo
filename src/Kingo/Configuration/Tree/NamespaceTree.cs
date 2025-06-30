using Kingo.Configuration.Spec;
using Kingo.Facts;
using LanguageExt;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Kingo.Configuration.Tree;

public sealed record NamespaceTree(
    Namespace Name,
    IReadOnlyDictionary<Relationship, RewriteNode> Relationships)
{
    public static async Task<NamespaceTree> FromFileAsync(string path) =>
        FromSpec(JsonSerializer.Deserialize<NamespaceSpec>(await File.ReadAllTextAsync(path))!);

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
                SubjectSetRewriteOperation o => OperationNode.From(
                    o.Operation,
                    o.Children.Select(ConvertRewrite).ToArray()
                ),
                _ => throw new NotSupportedException()
            };
}

public abstract record RewriteNode;

public sealed record ThisNode
    : RewriteNode
{
    public static ThisNode This { get; } = new ThisNode();
}

public sealed record ComputedSubjectSetNode(Relationship Relationship)
    : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputedSubjectSetNode From(Relationship relationship) => new(relationship);
}

public sealed record UnionNode(
    IReadOnlyList<RewriteNode> Children)
    : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionNode From(RewriteNode[] children) => new(children);
}

public sealed record IntersectionNode(
    IReadOnlyList<RewriteNode> Children)
    : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionNode From(RewriteNode[] children) => new(children);
}

public sealed record ExclusionNode(
    RewriteNode Include,
    RewriteNode Exclude)
    : RewriteNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExclusionNode From(RewriteNode include, RewriteNode exclude) => new(include, exclude);
}
