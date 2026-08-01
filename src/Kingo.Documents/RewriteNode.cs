using System.Collections.Immutable;

namespace Kingo.Documents;

internal abstract class RewriteNode
{
    private RewriteNode() { }

    internal sealed class This
        : RewriteNode
    {
        public static This Instance { get; } = new();

        private This() { }
    }

    internal sealed class ComputedSubjectSet(string relation)
        : RewriteNode
    {
        public string Relation { get; } = relation;
    }

    internal sealed class FactToSubjectSet(string factsetRelation, string computedSubjectSetRelation)
        : RewriteNode
    {
        public string FactsetRelation { get; } = factsetRelation;

        public string ComputedSubjectSetRelation { get; } = computedSubjectSetRelation;
    }

    internal sealed class Union(ImmutableArray<RewriteNode> children)
        : RewriteNode
    {
        public ImmutableArray<RewriteNode> Children { get; } = children;
    }

    internal sealed class Intersection(ImmutableArray<RewriteNode> children)
        : RewriteNode
    {
        public ImmutableArray<RewriteNode> Children { get; } = children;
    }

    internal sealed class Exclusion(RewriteNode include, RewriteNode exclude)
        : RewriteNode
    {
        public RewriteNode Include { get; } = include;

        public RewriteNode Exclude { get; } = exclude;
    }
}
