namespace Kingo.Acl;

/// <summary>
/// The result of an Expand evaluation — the full subject-set rewrite tree materialized for a <see cref="Kingo.Graphs.SubjectSet"/>, as opposed to
/// Check's short-circuiting boolean verdict (<see cref="Decision"/>). Expected to carry the effective set of subjects and the tree structure that
/// derived it, evaluated at a snapshot.
/// Shape TBD — stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Expansion;
