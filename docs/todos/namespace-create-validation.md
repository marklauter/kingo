---
title: Namespace.Create validation — cycles and dangling references
summary: "Namespace.Create rejects rewrite defects at construction: cycles in the ComputedSubjectSetRewrite reference graph, and dangling intra-namespace references (computed subjectset targets, tupleset first elements). Makes \"the evaluator never meets a schema cycle\" an invariant."
tags: [note, todo, schemas, validation]
created: 2026-07-18
status: open
priority: high
effort: medium
supports: "[[rewrite-interpreters]]"
---

# Namespace.Create validation — cycles and dangling references

Ruled (Mark, 2026-07-18): an unhealthy schema is detected at construction, so `Contains` carries no schema-cycle guard and its depth bound counts only fact-driven re-entries ([[rewrite-interpreters]], condition 3). Today neither `Schema.Create` nor `Namespace.Create` validates rewrite references at all — a `Namespace` with cycles or dangling references constructs successfully. This work closes that.

## What to validate, per namespace, at construction

1. **Cycle detection over the zero-fact recursion graph.** Nodes are the namespace's relationships; edges are `ComputedSubjectSetRewrite` references, collected through union/intersection/exclusion nesting. Self-reference is the trivial cycle. Tupleset arms are excluded: a `TupleToSubjectSetRewrite` cannot recurse without consuming a stored fact, so it belongs to the depth bound, not this check. The graph is intra-namespace by construction — `ComputedSubjectSetRewrite` carries only a `RelationshipIdentifier` and roots at the same resource.
2. **Dangling references.** Every `ComputedSubjectSetRewrite.Relationship` and every `TupleToSubjectSetRewrite.TuplesetRelationship` names a relationship defined in this namespace.
3. **Empty operator nodes** (ruled Mark, 2026-07-19). `UnionRewrite`/`IntersectionRewrite` with empty operand lists are refused. The SDL grammar cannot produce the shape; the Write API path can, and the conventional reading of an empty intersection is the universal set — everyone a member — so the shape is refused rather than given semantics.

Not validatable here: `TupleToSubjectSetRewrite.ComputedSubjectSetRelationship`. Its target namespace is unknown until facts resolve the tupleset's subjects, so an undefined relationship there stays condition 4 (undefined namespace or relationship mid-walk — broadened and demoted to a never-in-practice backstop by the drift ruling, 2026-07-20; [[rewrite-interpreters-findings]] F8) in the interpreter's taxonomy.

## Why construction, not SDL parse

The Write API builds `Schema` values without touching `Kingo.Sdl`; a parse-time guard leaves that path open. Guarding `Namespace.Create` covers every producer, and SDL inherits it.

## Open

- Which factory owns the checks — a `Result`-returning `Parse` beside a trusted `Create`, or validation folded into the existing factory — follows the house boundary rule; decide at implementation. Check 3 may belong on the operator constructors themselves rather than on `Namespace.Create`; same decision point.
