using Results;
using System.Collections.Immutable;

namespace Kingo.Domains;

/// <summary>
/// A namespace's definition <b>as a value</b>: an immutable snapshot of its relationships and their rewrites, with structural equality. Parse-agnostic and
/// storable. An entity within the <see cref="Domain"/> aggregate, not a root. Its identity is local: <see cref="Name"/> is unique within its domain. Names arrive
/// canonical lowercase through <c>Parse</c>, and the comparison here is ordinal. It is immutable, so there is no rename, only a new namespace.
/// <see cref="Create"/> is the only construction path, so a <c>Namespace</c> that exists satisfies its invariants. Entity-ness (versioning, lifecycle, optimistic
/// concurrency, authorship) is the Write context's wrapper and never lives in core. If this type ever grows a version field, a timestamp, or a mutation method,
/// it has crossed the line and belongs to a service ([[domain-language]]).
/// </summary>
public sealed record Namespace
{
    public NamespaceName Name { get; }

    public ImmutableArray<Relationship> Relationships { get; }

    private Namespace(NamespaceName name, ImmutableArray<Relationship> relationships)
    {
        Name = name;
        Relationships = relationships;
    }

    /// <summary>
    /// Constructs a namespace from its name and relationships, validating for untrusted and trusted callers alike. The checks are staged because each makes the
    /// next well-defined: duplicates make reference resolution ambiguous, and dangling references make the cycle graph ill-defined. Each stage accumulates every
    /// <see cref="ErrorType.Validation"/> error it finds before returning. The domain model has no core <c>Parse</c>. Its text forms live in serialization adapters,
    /// which call this after decoding ([[domain-language]]). The only construction path.
    /// </summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the namespace when every stage passes. Otherwise a failure carrying, in order: duplicate relationship names
    /// (<c>namespace.duplicate_relationship</c>, one error per duplicated name in first-occurrence order); then dangling intra-namespace references
    /// (<c>namespace.dangling_reference</c>, where every <see cref="SubjectSetRewrite.ComputedSubjectSet.Relationship"/> and every
    /// <see cref="SubjectSetRewrite.FactToSubjectSet.FactsetRelationship"/> names a relationship defined here, while the factset's
    /// <see cref="SubjectSetRewrite.FactToSubjectSet.ComputedSubjectSetRelationship"/> targets another namespace and stays the interpreter's condition 4); then
    /// cycles in the zero-fact recursion graph (<c>namespace.rewrite_cycle</c>, each error carrying the full cycle path, where edges are
    /// <see cref="SubjectSetRewrite.ComputedSubjectSet"/> references, and factset arms cannot recurse without consuming a stored fact, so they belong to the
    /// evaluator's depth bound, not this check; [[rewrite-interpreters]]).
    /// </returns>
    public static Result<Namespace> Create(NamespaceName name, ImmutableArray<Relationship> relationships)
    {
        // a default array is the empty namespace: normalized here so construction is total and the stored value always enumerates
        if (relationships.IsDefault)
            relationships = [];

        var duplicates = relationships
            .GroupBy(relationship => relationship.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "namespace.duplicate_relationship",
                $"relationship '{group.Key}' is defined more than once in namespace '{name}'"))
            .ToImmutableArray();
        if (!duplicates.IsEmpty)
            return Result.Failure<Namespace>(duplicates);

        // one tree walk per relationship: the references materialize here and both remaining stages consume them
        var references = relationships.ToImmutableDictionary(
            relationship => relationship.Name,
            relationship => IntraNamespaceReferences(relationship.Rewrite).Distinct().ToImmutableArray());

        var defined = relationships.Select(relationship => relationship.Name).ToImmutableHashSet();
        var dangling = relationships
            .SelectMany(relationship => references[relationship.Name]
                .Select(reference => reference.Target)
                // not redundant with the tuple-level Distinct above: one target can appear under both edge kinds
                .Distinct()
                .Where(target => !defined.Contains(target))
                .Select(target => Error.Validation(
                    "namespace.dangling_reference",
                    $"relationship '{relationship.Name}' references '{target}', which is not defined in namespace '{name}'")))
            .ToImmutableArray();
        if (!dangling.IsEmpty)
            return Result.Failure<Namespace>(dangling);

        var cycles = DetectCycles(name, relationships, references);
        return cycles.IsEmpty
            ? Result.Success(new Namespace(name, relationships))
            : Result.Failure<Namespace>(cycles);
    }

    /// <summary>
    /// Yields every node of a rewrite tree in tree order, the root first. Operator nesting is flattened, and factset arms are terminal because they hold no nested
    /// rewrites. Uses an explicit stack rather than recursion: this runs on untrusted input, and a modeled-error gate must not let input depth pick its stack depth.
    /// </summary>
    /// <returns>The rewrite's nodes in tree order, root first.</returns>
    private static IEnumerable<SubjectSetRewrite> Flatten(SubjectSetRewrite rewrite)
    {
        var pending = new Stack<SubjectSetRewrite>();
        pending.Push(rewrite);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            yield return node;
            var children = Children(node);
            for (var i = children.Length - 1; i >= 0; i--)
                pending.Push(children[i]);
        }
    }

    /// <summary>Returns the operand list of an operator node. Leaves have none. Exhaustive over the closed hierarchy, so every traversal built on it is too.</summary>
    /// <returns>The node's operands, or an empty array for a leaf.</returns>
    private static ImmutableArray<SubjectSetRewrite> Children(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            SubjectSetRewrite.Union union => union.Children,
            SubjectSetRewrite.Intersection intersection => intersection.Children,
            SubjectSetRewrite.Exclusion exclusion => [exclusion.Include, exclusion.Exclude],
            SubjectSetRewrite.This or SubjectSetRewrite.ComputedSubjectSet => [],
            // the last inhabitant of the closed hierarchy: a discard arm with a cast (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch, and fails
            // loudly if the union ever grows a variant this traversal has not met
            _ => LeafChildren((SubjectSetRewrite.FactToSubjectSet)rewrite),
        };

    private static ImmutableArray<SubjectSetRewrite> LeafChildren(SubjectSetRewrite.FactToSubjectSet _) => [];

    /// <summary>
    /// Returns the relationships a rewrite names within its own namespace. One extraction feeds both validation stages, so the two can never disagree on what a
    /// reference is. Computed subject-set targets are zero-fact edges: they recurse without consuming a stored fact, so they feed the cycle stage. Factset first
    /// elements are references only, because a factset hop consumes a fact, so it counts against the evaluator's depth bound instead. The factset's second element
    /// resolves in another namespace and is not referenced here.
    /// </summary>
    /// <returns>Each referenced relationship name paired with whether it is a zero-fact edge.</returns>
    private static IEnumerable<(RelationshipName Target, bool IsZeroFactEdge)> IntraNamespaceReferences(SubjectSetRewrite rewrite) =>
        Flatten(rewrite).SelectMany(IEnumerable<(RelationshipName, bool)> (node) => node switch
        {
            SubjectSetRewrite.ComputedSubjectSet computed => [(computed.Relationship, true)],
            SubjectSetRewrite.FactToSubjectSet factTo => [(factTo.FactsetRelationship, false)],
            // operator nodes carry no references of their own; an unrecognized variant already failed loudly in Children
            _ => [],
        });

    /// <summary>
    /// Searches the zero-fact recursion graph depth-first for cycles. Nodes are the namespace's relationships, and edges are its
    /// <see cref="SubjectSetRewrite.ComputedSubjectSet"/> references. Reports one error per back edge the search meets, not one per elementary cycle: cycles
    /// sharing a node can collapse into one report, and a defective domain always fails, though fixing one cycle can surface another. Each error carries the full
    /// cycle path, so the domain is diagnosable without re-deriving the graph. Runs after the dangling-reference stage, so every edge target is a defined node. Uses
    /// mutable three-color bookkeeping with an explicit frame stack rather than an expression pipeline or recursion: the path that makes the error message is
    /// inherently stateful, and this runs on untrusted input, so input shape must not pick the stack depth.
    /// </summary>
    /// <returns>One <see cref="Error"/> per back edge found, empty when the graph is acyclic.</returns>
    private static ImmutableArray<Error> DetectCycles(
        NamespaceName name,
        ImmutableArray<Relationship> relationships,
        ImmutableDictionary<RelationshipName, ImmutableArray<(RelationshipName Target, bool IsZeroFactEdge)>> references)
    {
        var edges = references.ToImmutableDictionary(
            entry => entry.Key,
            entry => entry.Value
                .Where(reference => reference.IsZeroFactEdge)
                .Select(reference => reference.Target)
                .ToImmutableArray());

        var errors = ImmutableArray.CreateBuilder<Error>();
        var finished = new HashSet<RelationshipName>();
        var path = new List<RelationshipName>();
        var onPath = new HashSet<RelationshipName>();
        var frames = new Stack<(RelationshipName Node, int NextEdge)>();

        foreach (var relationship in relationships)
        {
            if (finished.Contains(relationship.Name))
                continue;

            frames.Push((relationship.Name, 0));
            _ = onPath.Add(relationship.Name);
            path.Add(relationship.Name);

            while (frames.Count > 0)
            {
                var (node, nextEdge) = frames.Pop();
                var targets = edges[node];
                if (nextEdge == targets.Length)
                {
                    path.RemoveAt(path.Count - 1);
                    _ = onPath.Remove(node);
                    _ = finished.Add(node);
                    continue;
                }

                frames.Push((node, nextEdge + 1));
                var target = targets[nextEdge];
                if (finished.Contains(target))
                    continue;

                if (onPath.Contains(target))
                {
                    var cycle = path.Skip(path.IndexOf(target)).Append(target);
                    errors.Add(Error.Validation(
                        "namespace.rewrite_cycle",
                        $"rewrite cycle in namespace '{name}': {string.Join(" -> ", cycle.Select(step => $"'{step}'"))}"));
                    continue;
                }

                frames.Push((target, 0));
                _ = onPath.Add(target);
                path.Add(target);
            }
        }

        return errors.ToImmutable();
    }

    public bool Equals(Namespace? other) =>
        other is not null
        && Name.Equals(other.Name)
        && Relationships.AsSpan().SequenceEqual(other.Relationships.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, SequenceHash.Of(Relationships));
}
