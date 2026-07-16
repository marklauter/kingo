using Results;
using System.Collections.Immutable;

namespace Kingo.Graphs;

/// <summary>
/// A graph <b>as a value</b> — the set of facts that exist, immutable with structural equality (element-wise,
/// order-sensitive over <see cref="Facts"/>). What <see cref="Kingo.Schemas.Schema"/> is to the rules, <c>Graph</c>
/// is to the ground data: the schema defines what edges may exist, the graph is the edges that do exist, and Check
/// walks it. <see cref="Create"/> is the only construction path, so a <c>Graph</c> that exists satisfies its
/// invariants.
/// <para>
/// Unlike <see cref="Kingo.Schemas.Schema"/>, an <b>empty graph is legal</b>: a schema with no namespaces is the
/// absence of a schema, but a graph with no facts is a real state — nothing has been asserted yet, and every Check
/// against it correctly denies. Note this contradicts the guardrail in docs/notes/domain-language.md saying
/// <c>Graph</c> names a concept and never a domain value; the note needs revisiting.
/// </para>
/// </summary>
public sealed record Graph
{
    public ImmutableArray<Fact> Facts { get; }

    private Graph(ImmutableArray<Fact> facts) => Facts = facts;

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike: rejects duplicate facts
    /// (<c>graph.duplicate_fact</c>, one <see cref="ErrorType.Validation"/> error per duplicated fact in first-occurrence order —
    /// a fact's domain key is the whole triple, so asserting one twice says nothing the first assertion did not). An empty fact
    /// set is accepted; <c>default(ImmutableArray&lt;Fact&gt;)</c> normalizes to it, since the absence of an array and the
    /// absence of facts are the same graph and the default instance would otherwise throw on first enumeration.
    /// </summary>
    public static Result<Graph> Create(ImmutableArray<Fact> facts)
    {
        if (facts.IsDefaultOrEmpty)
            return Result.Success(new Graph([]));

        var duplicates = facts
            .GroupBy(fact => fact)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "graph.duplicate_fact",
                $"fact '{group.Key}' is asserted more than once in the graph"))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Graph(facts))
            : Result.Failure<Graph>(duplicates);
    }

    public bool Equals(Graph? other) =>
        other is not null
        && Facts.AsSpan().SequenceEqual(other.Facts.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var fact in Facts)
            hash.Add(fact);
        return hash.ToHashCode();
    }
}
