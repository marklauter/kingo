using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// A namespace's definition <b>as a value</b> — an immutable snapshot of its relationships and their rewrites,
/// with structural equality. Parse-agnostic and storable. An entity within the <see cref="Spec"/> aggregate,
/// not a root: its identity is local — <see cref="Name"/> unique within its spec (names arrive canonical lowercase
/// through <c>Parse</c>; the comparison here is ordinal), immutable
/// (there is no rename, only a new namespace). <see cref="Create"/> is the only construction path, so a
/// <c>Namespace</c> that exists satisfies its invariants. Entity-ness (versioning, lifecycle, optimistic
/// concurrency, authorship) is the Write context's wrapper and never lives in core: if this type ever grows
/// a version field, a timestamp, or a mutation method, it has crossed the line and belongs to a service
/// ([[domain-language]]).
/// </summary>
public sealed record Namespace
{
    public NamespaceIdentifier Name { get; }

    public ImmutableArray<Relationship> Relationships { get; }

    private Namespace(NamespaceIdentifier name, ImmutableArray<Relationship> relationships)
    {
        Name = name;
        Relationships = relationships;
    }

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike, staged because each check makes the next well-defined
    /// (duplicates make reference resolution ambiguous; dangling references make the cycle graph ill-defined), each stage accumulating every
    /// <see cref="ErrorType.Validation"/> error it finds before returning:
    /// duplicate relationship names (<c>namespace.duplicate_relationship</c>, one error per duplicated name in first-occurrence order), then dangling
    /// intra-namespace references (<c>namespace.dangling_reference</c> — every <see cref="ComputedSubjectSetRewrite.Relationship"/> and every
    /// <see cref="FactToSubjectSetRewrite.FactsetRelationship"/> names a relationship defined here; the factset's
    /// <see cref="FactToSubjectSetRewrite.ComputedSubjectSetRelationship"/> targets another namespace and stays the interpreter's condition 4), then cycles in
    /// the zero-fact recursion graph (<c>namespace.rewrite_cycle</c>, each error carrying the full cycle path — edges are
    /// <see cref="ComputedSubjectSetRewrite"/> references; factset arms cannot recurse without consuming a stored fact, so they belong to the evaluator's depth
    /// bound, not this check; [[rewrite-interpreters]]). The spec model has no core <c>Parse</c> — its text forms live in serialization adapters, which call
    /// this after decoding ([[domain-language]]).
    /// </summary>
    public static Result<Namespace> Create(NamespaceIdentifier name, ImmutableArray<Relationship> relationships)
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
    /// Every node of a rewrite tree in tree order, the root first — operator nesting flattened, factset arms terminal (they hold no nested rewrites).
    /// An explicit stack rather than recursion: this runs on untrusted input, and a modeled-error gate must not let input depth pick its stack depth.
    /// </summary>
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

    /// <summary>The operand list of an operator node; leaves have none. Exhaustive over the closed hierarchy so every traversal built on it is too.</summary>
    private static ImmutableArray<SubjectSetRewrite> Children(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            UnionRewrite union => union.Children,
            IntersectionRewrite intersection => intersection.Children,
            ExclusionRewrite exclusion => [exclusion.Include, exclusion.Exclude],
            ThisRewrite or ComputedSubjectSetRewrite => [],
            // the last inhabitant of the closed hierarchy: a discard arm with a cast (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch, and fails
            // loudly if the union ever grows a variant this traversal has not met
            _ => LeafChildren((FactToSubjectSetRewrite)rewrite),
        };

    private static ImmutableArray<SubjectSetRewrite> LeafChildren(FactToSubjectSetRewrite _) => [];

    /// <summary>
    /// The relationships a rewrite names within its own namespace — one extraction feeding both validation stages, so the two can never disagree on what a
    /// reference is. Computed subjectset targets are zero-fact edges (they recurse without consuming a stored fact, so they feed the cycle stage); factset
    /// first elements are references only (a factset hop consumes a fact, so it counts against the evaluator's depth bound instead). The factset's second
    /// element resolves in another namespace and is not referenced here.
    /// </summary>
    private static IEnumerable<(RelationshipIdentifier Target, bool IsZeroFactEdge)> IntraNamespaceReferences(SubjectSetRewrite rewrite) =>
        Flatten(rewrite).SelectMany(IEnumerable<(RelationshipIdentifier, bool)> (node) => node switch
        {
            ComputedSubjectSetRewrite computed => [(computed.Relationship, true)],
            FactToSubjectSetRewrite factTo => [(factTo.FactsetRelationship, false)],
            // operator nodes carry no references of their own; an unrecognized variant already failed loudly in Children
            _ => [],
        });

    /// <summary>
    /// Depth-first search over the zero-fact recursion graph — nodes are the namespace's relationships, edges its <see cref="ComputedSubjectSetRewrite"/>
    /// references — reporting one error per back edge the search meets (not one per elementary cycle: cycles sharing a node can collapse into one report;
    /// a defective spec always fails, but fixing one cycle can surface another), each error carrying the full cycle path so the spec is diagnosable
    /// without re-deriving the graph. Runs after the dangling-reference stage, so every edge target is a defined node. Mutable three-color bookkeeping with an
    /// explicit frame stack rather than an expression pipeline or recursion: the path that makes the error message is inherently stateful, and this runs on
    /// untrusted input, so input shape must not pick the stack depth.
    /// </summary>
    private static ImmutableArray<Error> DetectCycles(
        NamespaceIdentifier name,
        ImmutableArray<Relationship> relationships,
        ImmutableDictionary<RelationshipIdentifier, ImmutableArray<(RelationshipIdentifier Target, bool IsZeroFactEdge)>> references)
    {
        var edges = references.ToImmutableDictionary(
            entry => entry.Key,
            entry => entry.Value
                .Where(reference => reference.IsZeroFactEdge)
                .Select(reference => reference.Target)
                .ToImmutableArray());

        var errors = ImmutableArray.CreateBuilder<Error>();
        var finished = new HashSet<RelationshipIdentifier>();
        var path = new List<RelationshipIdentifier>();
        var onPath = new HashSet<RelationshipIdentifier>();
        var frames = new Stack<(RelationshipIdentifier Node, int NextEdge)>();

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

    public override int GetHashCode() => HashCode.Combine(Name, RewriteHash.OfSequence(Relationships));
}
