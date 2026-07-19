namespace Kingo.Closures;

/// <summary>
/// The result of an Expand evaluation — the full subject-set rewrite tree materialized for a <see cref="Kingo.Graphs.Subject.SubjectSet"/>, as opposed to
/// Contains's short-circuiting boolean verdict (<see cref="Decision"/>). Expected to carry the <see cref="Kingo.Graphs.Subject.SubjectSet"/> asked about, the
/// materialized rewrite tree (referenced subject sets stay leaves — single-level), the snapshot pin, the schema version, and the wall timestamp — the
/// same five-slot shape as <see cref="Decision"/>, with a tree in the seat the verdict occupies.
/// Shape TBD — stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Expansion;
