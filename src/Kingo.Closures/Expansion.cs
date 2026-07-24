namespace Kingo.Closures;

/// <summary>
/// The result of an Expand evaluation. The full subject-set rewrite tree materialized for a <see cref="Kingo.Graphs.SubjectSet"/>,
/// where Contains yields a short-circuiting boolean verdict (<see cref="Decision"/>). Expected to carry the
/// <see cref="Kingo.Graphs.SubjectSet"/> asked about, the materialized rewrite tree, the snapshot pin, the spec version, and the
/// wall timestamp. Referenced subject sets stay leaves, so the tree is single-level. This is the same five-slot shape as
/// <see cref="Decision"/>, with a tree in the seat the verdict occupies.
/// Shape to be determined. Stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Expansion;
