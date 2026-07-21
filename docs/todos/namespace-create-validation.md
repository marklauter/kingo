---
title: Namespace.Create validation — cycles and dangling references
summary: "Namespace.Create rejects rewrite defects at construction: cycles in the ComputedSubjectSetRewrite reference graph, and dangling intra-namespace references (computed subjectset targets, factset first elements). Makes \"the evaluator never meets a schema cycle\" an invariant."
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

1. **Cycle detection over the zero-fact recursion graph.** Nodes are the namespace's relationships; edges are `ComputedSubjectSetRewrite` references, collected through union/intersection/exclusion nesting. Self-reference is the trivial cycle. Factset arms are excluded: a `FactToSubjectSetRewrite` cannot recurse without consuming a stored fact, so it belongs to the depth bound, not this check. The graph is intra-namespace by construction — `ComputedSubjectSetRewrite` carries only a `RelationshipIdentifier` and roots at the same resource.
2. **Dangling references.** Every `ComputedSubjectSetRewrite.Relationship` and every `FactToSubjectSetRewrite.FactsetRelationship` names a relationship defined in this namespace.
3. **Empty operator nodes** (ruled Mark, 2026-07-19). `UnionRewrite`/`IntersectionRewrite` with empty operand lists are refused. The SDL grammar cannot produce the shape; the Write API path can, and the conventional reading of an empty intersection is the universal set — everyone a member — so the shape is refused rather than given semantics.

Not validatable here: `FactToSubjectSetRewrite.ComputedSubjectSetRelationship`. Its target namespace is unknown until facts resolve the factset's resources, so an undefined relationship there stays condition 4 (undefined namespace or relationship mid-walk — broadened and demoted to a never-in-practice backstop by the drift ruling, 2026-07-20; [[rewrite-interpreters-findings]] F8) in the interpreter's taxonomy.

## Why construction, not SDL parse

The Write API builds `Schema` values without touching `Kingo.Sdl`; a parse-time guard leaves that path open. Guarding `Namespace.Create` covers every producer, and SDL inherits it.

## Ruled at implementation (Mark, 2026-07-21)

- **Factory shape.** `Namespace.Create` is already the `Result`-returning, error-accumulating only-construction-path; all three checks fold into it. No `Parse` beside it.
- **Check 3 home: the namespace gate, not the rewrites** (revised 2026-07-21, superseding the same-day ruling for operator constructors). Rewrites are not standalone entities; they exist only within their aggregate, and nothing consumes a rewrite outside a namespace. Checks 1 and 2 already walk every operator node to collect references, so the empty-operand refusal is a free predicate on that walk. Rewrite types stay plain records; no private-constructor ripple.
- **Storage direction, deferred.** Grammar as the storage format — schemas persisted as SDL text, sqlite-style — is the leaning but not yet ruled. Under it, hydration is parsing, so the namespace gate runs on every load and no trusted fast-construction path exists to design. The printer and round-trip law already exist (`SchemaPrinter`, `SchemaRoundTripTests`). This todo's design is identical under either storage answer; the decision becomes urgent when the Write service persists its first schema, and it decides together with [[storage-versioning-design]].
- **Check ordering: staged, not accumulated.** Duplicates make reference resolution ambiguous, and dangling references make the cycle graph ill-defined. The checks stage: duplicates, then dangling references, then cycles.
- **Cycle errors carry the full cycle path**, so a defective schema is diagnosable from the user's chair without re-deriving the graph.
