namespace Kingo.Closures;

/// <summary>
/// A materialized rewrite tree, recorded with everything its replay needs. Where Contains judges one membership question and
/// yields a short-circuiting verdict (<see cref="Decision"/>), Expand materializes one relation's rewrite tree whole. Expected to
/// carry the <see cref="Kingo.Facts.SubjectSet"/> asked about, the materialized rewrite tree, the snapshot pin, the theory
/// version, and the wall timestamp. Referenced subjectsets stay leaves, so the tree is single-level. This is the same five-slot
/// shape as <see cref="Decision"/>, with a tree in the seat the verdict occupies.
/// Shape to be determined. Stub capturing the term ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Expansion;
