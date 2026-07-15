using System.Collections.Immutable;

namespace Kingo.Sdl;

/// <summary>
/// The parser-internal syntax tree for rewrite expressions — the shape Superpower produces before identifiers have crossed the trust boundary. Leaves carry raw
/// <see cref="string"/>s; <see cref="RewriteExpressionParser"/> transforms the tree into the core <c>SubjectSetRewrite</c> algebra at its exit, parsing every
/// identifier through <c>RelationshipIdentifier.Parse</c>. Build-once, transform-once: nodes are never compared, so these are plain sealed classes, not
/// records.
/// </summary>
internal abstract class RewriteNode
{
    private protected RewriteNode() { }
}

/// <summary>The <c>this</c> keyword — direct membership.</summary>
internal sealed class ThisNode : RewriteNode
{
    public static ThisNode Instance { get; } = new();

    private ThisNode() { }
}

/// <summary>A bare identifier — another relationship on the same resource.</summary>
internal sealed class ComputedNode(string relationship) : RewriteNode
{
    public string Relationship { get; } = relationship;
}

/// <summary>A <c>(tupleset, computed)</c> pair — a walk through a tupleset relationship.</summary>
internal sealed class TupleToNode(string tuplesetRelationship, string computedRelationship) : RewriteNode
{
    public string TuplesetRelationship { get; } = tuplesetRelationship;

    public string ComputedRelationship { get; } = computedRelationship;
}

/// <summary>A run of <c>|</c>-joined operands.</summary>
internal sealed class UnionNode(ImmutableArray<RewriteNode> children) : RewriteNode
{
    public ImmutableArray<RewriteNode> Children { get; } = children;
}

/// <summary>A run of <c>&amp;</c>-joined operands.</summary>
internal sealed class IntersectionNode(ImmutableArray<RewriteNode> children) : RewriteNode
{
    public ImmutableArray<RewriteNode> Children { get; } = children;
}

/// <summary>An <c>include ! exclude</c> pair.</summary>
internal sealed class ExclusionNode(RewriteNode include, RewriteNode exclude) : RewriteNode
{
    public RewriteNode Include { get; } = include;

    public RewriteNode Exclude { get; } = exclude;
}
