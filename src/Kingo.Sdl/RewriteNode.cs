using System.Collections.Immutable;

namespace Kingo.Sdl;

/// <summary>
/// The parser-internal syntax tree for rewrite expressions — the shape Superpower produces before identifiers have crossed the trust boundary. Leaves carry raw
/// <see cref="string"/>s; <see cref="RewriteExpressionParser"/> transforms the tree into the core <c>SubjectSetRewrite</c> algebra at its exit, parsing every
/// identifier through <c>RelationshipName.Parse</c>. Build-once, transform-once: nodes are never compared, so these are plain sealed classes, not records —
/// reference equality is the right identity for a tree built once and consumed once, and it keeps <see cref="This"/> a genuine singleton.
/// <para>
/// The cases nest under the base and the base constructor is private, so the case set is closed by the compiler, not by convention — no seventh inhabitant is
/// declarable anywhere. Each case is named for its <c>SubjectSetRewrite</c> counterpart exactly — <c>RewriteNode.Union</c> lifts to <c>SubjectSetRewrite.Union</c>
/// — so <see cref="RewriteExpressionParser.Transform"/> reads as a lift, not a translation table. The tier difference is the raw <see cref="string"/> leaves, not
/// the vocabulary; the property types carry it.
/// </para>
/// </summary>
internal abstract class RewriteNode
{
    private RewriteNode() { }

    /// <summary>The <c>this</c> keyword — direct membership.</summary>
    internal sealed class This
        : RewriteNode
    {
        public static This Instance { get; } = new();

        private This() { }
    }

    /// <summary>A bare identifier — another relationship on the same resource.</summary>
    internal sealed class ComputedSubjectSet(string relationship)
        : RewriteNode
    {
        public string Relationship { get; } = relationship;
    }

    /// <summary>A <c>(factset, computed)</c> pair — a walk through a factset relationship.</summary>
    internal sealed class FactToSubjectSet(string factsetRelationship, string computedSubjectSetRelationship)
        : RewriteNode
    {
        public string FactsetRelationship { get; } = factsetRelationship;

        public string ComputedSubjectSetRelationship { get; } = computedSubjectSetRelationship;
    }

    /// <summary>A run of <c>|</c>-joined operands.</summary>
    internal sealed class Union(ImmutableArray<RewriteNode> children)
        : RewriteNode
    {
        public ImmutableArray<RewriteNode> Children { get; } = children;
    }

    /// <summary>A run of <c>&amp;</c>-joined operands.</summary>
    internal sealed class Intersection(ImmutableArray<RewriteNode> children)
        : RewriteNode
    {
        public ImmutableArray<RewriteNode> Children { get; } = children;
    }

    /// <summary>An <c>include ! exclude</c> pair.</summary>
    internal sealed class Exclusion(RewriteNode include, RewriteNode exclude)
        : RewriteNode
    {
        public RewriteNode Include { get; } = include;

        public RewriteNode Exclude { get; } = exclude;
    }
}
