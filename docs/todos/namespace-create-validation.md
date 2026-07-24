---
title: Namespace.Create validation — cycles and dangling references
summary: "Namespace.Create rejects rewrite defects at construction: cycles in the SubjectSetRewrite.ComputedSubjectSet reference graph, and dangling intra-namespace references (computed subjectset targets, factset first elements). Makes \"the evaluator never meets a spec cycle\" an invariant."
tags: [note, todo, schemas, validation]
created: 2026-07-18
status: closed
priority: high
supports: "[[rewrite-interpreters]]"
---

# Namespace.Create validation — cycles and dangling references

Ruled (Mark, 2026-07-18): an unhealthy spec is detected at construction, so `Contains` carries no spec-cycle guard and its depth bound counts only fact-driven re-entries ([[rewrite-interpreters]], condition 3). Today neither `Spec.Create` nor `Namespace.Create` validates rewrite references at all — a `Namespace` with cycles or dangling references constructs successfully. This work closes that.

## What to validate, per namespace, at construction

1. **Cycle detection over the zero-fact recursion graph.** Nodes are the namespace's relationships; edges are `SubjectSetRewrite.ComputedSubjectSet` references, collected through union/intersection/exclusion nesting. Self-reference is the trivial cycle. Factset arms are excluded: a `SubjectSetRewrite.FactToSubjectSet` cannot recurse without consuming a stored fact, so it belongs to the depth bound, not this check. The graph is intra-namespace by construction — `SubjectSetRewrite.ComputedSubjectSet` carries only a `RelationshipPath` and roots at the same resource.
2. **Dangling references.** Every `SubjectSetRewrite.ComputedSubjectSet.Relationship` and every `SubjectSetRewrite.FactToSubjectSet.FactsetRelationship` names a relationship defined in this namespace.
3. **Empty operator nodes** (ruled Mark, 2026-07-19). `SubjectSetRewrite.Union`/`SubjectSetRewrite.Intersection` with empty operand lists are refused. The SDL grammar cannot produce the shape; the Write API path can, and the conventional reading of an empty intersection is the universal set — everyone a member — so the shape is refused rather than given semantics.

Not validatable here: `SubjectSetRewrite.FactToSubjectSet.ComputedSubjectSetRelationship`. Its target namespace is unknown until facts resolve the factset's resources, so an undefined relationship there stays condition 4 (undefined namespace or relationship mid-walk — broadened and demoted to a never-in-practice backstop by the drift ruling, 2026-07-20; dry-run finding F8) in the interpreter's taxonomy.

## Why construction, not SDL parse

The Write API builds `Spec` values without touching `Kingo.Sdl`; a parse-time guard leaves that path open. Guarding `Namespace.Create` covers every producer, and SDL inherits it.

## Ruled at implementation (Mark, 2026-07-21)

- **Factory shape.** `Namespace.Create` is already the `Result`-returning, error-accumulating only-construction-path; checks 1 and 2 fold into it. No `Parse` beside it.
- **Check 3 home: the rewrites protect themselves** (re-affirmed 2026-07-21 after a detour through the namespace gate). Every rewrite takes the private-constructor-plus-static-`Create` shape. `SubjectSetRewrite.Union` and `SubjectSetRewrite.Intersection` return `Result` and refuse empty operand lists — the invariant that can fail lives where it can fail. The infallible rewrites return the bare type: a `Result` on a construction that cannot fail claims a fallibility that does not exist. An empty operator node is unrepresentable, so `Namespace.Create` never needs check 3. The rewrites drop positional-record syntax for get-only properties with no `init` setters: `with` expressions bypass the factory and would reopen the door `Create` closes, so domain types do not expose them. Records remain for structural equality only. Every producer constructs through the `Create` methods — the SDL parser included: `RewriteExpressionParser` swaps `new` for `Create` at each node, `.Map` becoming `.Bind` at the two fallible operators, and the new failures join the parser's existing error accumulation.
- **Storage direction, deferred.** Grammar as the storage format — specs persisted as SDL text, sqlite-style — is the leaning but not yet ruled. Under it, hydration is parsing, so the namespace gate runs on every load and no trusted fast-construction path exists to design. The printer and round-trip law already exist (`SpecPrinter`, `SpecRoundTripTests`). This todo's design is identical under either storage answer; the decision becomes urgent when the Write service persists its first spec, and it decides together with [[storage-versioning-design]].
- **Check ordering: staged, not accumulated.** Duplicates make reference resolution ambiguous, and dangling references make the cycle graph ill-defined. The checks stage: duplicates, then dangling references, then cycles.
- **Cycle errors carry the full cycle path**, so a defective spec is diagnosable from the user's chair without re-deriving the graph.

## Resolution

Implemented 2026-07-21, branch `namespace-parse-invariants`.

- `src/Kingo.Schemas/Namespace.cs` — `Namespace.Create` stages duplicates (`namespace.duplicate_relationship`), dangling intra-namespace references (`namespace.dangling_reference` — computed subjectset targets and factset first elements; the factset's second element stays the interpreter's condition 4), then cycles over the `SubjectSetRewrite.ComputedSubjectSet` graph (`namespace.rewrite_cycle`, full cycle path in the message). Traversal and cycle search use explicit stacks so untrusted input cannot pick the stack depth.
- `src/Kingo.Schemas/SubjectSetRewrite.cs` — every rewrite is private-constructor-plus-static-`Create`, get-only properties, no `init`/`with` path; the operator factories return `Result`, refusing empty operand lists (`rewrite.union.empty` / `rewrite.intersection.empty`) and trees past `SubjectSetRewrite.MaxDepth` (`rewrite.depth`; the bound landed with [[rewrite-equality-recurses-unbounded]]); the leaves return the bare type.
- `src/Kingo.Sdl/RewriteExpressionParser.cs` — constructs through the `Create` factories, `.Map` → `.Bind` at the fallible operators; the SDL example in [[specs]] gained the `parent` definitions the gate now requires.
- Pinned by `tests/Kingo.Schemas.Tests/NamespaceTests.cs`, `SubjectSetRewriteTests.cs`, and the SDL suites (`SpecParseTests` carries parse-path `namespace.dangling_reference` / `namespace.rewrite_cycle` rows).

The storage-direction leaning (grammar as storage format) remains open and moves with [[storage-versioning-design]].
