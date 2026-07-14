---
type: todo
title: Kingo core test pass
summary: "The domain core is built but mostly untested — Namespace equality and Define validation have tests; the identifier and composite-parse surface has none, and the coverage ratchet (80/80/80) gates the solution. Next work item; the PDL adapter and IParse-keyed converters queue behind it."
tags: [note, todo, testing]
created: 2026-07-14
status: open
priority: high
effort: medium
disposition: delete-on-close
---

# Kingo core test pass

## Observation

`src/Kingo` was built fresh on 2026-07-14 per [[domain-language]]: four identifier IValues, the grammar compositions (`Resource`, `SubjectSet`, `Subject` DU, `Statement`), and the policy model (`Namespace`, `Relationship`, `SubjectSetRewrite` algebra). First tests landed the same day — `NamespaceTests` pins structural equality and `Namespace.Define`'s duplicate-name validation. The identifier and composite-parse surface is still untested, and the coverage ratchet (`Directory.Build.props`: 80/80/80 line/branch/method) gates the solution.

## Interpretation

What the test pass should pin, in rough priority order:

- **Identifier parse rules** — valid/invalid inputs per terminal regex; empty/whitespace/null failure paths with the `namespace_id.*` / `relationship_id.*` / `resource_id.*` / `subject_id.*` error codes; case normalization (`Doc` parses to `doc`) for `NamespaceIdentifier` and `RelationshipIdentifier`; `RelationshipIdentifier.Nothing` sentinel round-trips.
- **Delimiter reservation invariant** — `#` and `@` never valid inside any terminal; `:` only in `SubjectIdentifier`. The composite single-`IndexOf` parsing depends on this; a future regex tweak that admits a delimiter must trip a test, not a production incident.
- **Composite Parse/ToString inverse** — round-trip property for `Resource`, `SubjectSet`, `Subject` (both DU branches), `Statement` (canonical text is the fixture DSL: `Statement.Parse("doc:readme#viewer@user:anne")`).
- **Error accumulation** — a composite with two invalid parts fails with both errors (the `Result.Apply` validation-applicative behavior), not fail-fast.
- **Subject DU** — `#` branches to `SubjectSet`, otherwise `DirectSubject`; hierarchy closed (pattern-match exhaustiveness style per Results.Tests precedent).
- **Structural equality** — `Namespace` and `UnionRewrite`/`IntersectionRewrite` element-wise equality and hashing (the custom overrides required by [[immutablearray-for-domain-collections]]); consider the ArchUnit-style check from that note's Next section: records carrying `ImmutableArray` must override `Equals`/`GetHashCode`.
- **ArchUnit layout rules** — aggregate namespaces don't reference each other outside the allowed acyclic flow (`Statements` → `Subjects`/`Resources` → root); no serialization attributes on domain types; sealed concretes.

Style precedents: law tests in Results.Tests; the reflection-shape test in Values.Tests (`TryParse_IsDeclaredOnImplementorType`) for anything ITryParse-related.

## Next

1. Write the test pass; acceptance is `build-gate.sh` green (format, build, tests, ratchet). The script isn't a repo file — it's the canonical gate from the writing-csharp skill (`csharp` plugin, `skills/writing-csharp/scripts/build-gate.sh`).
2. Then **`Kingo.Serialization.Pdl`** — the step that makes the core reachable: today nothing can produce a `Namespace` value except hand-written C#, and policy authoring is the front door. New project + `Kingo.Serialization.Pdl.Tests`; salvage the Superpower rewrite-expression grammar and YamlDotNet structure from the `Kingo.Pdl` quarry as reference; parser-internal AST stays `internal`, transform exits into `Namespace`/`Relationship`/`SubjectSetRewrite`; all errors as `Result` failures (no `PdlParseException` at the boundary). Acceptance: PDL text → domain values → PDL text round-trips. Details in [[dissolve-kingo-pdl-under-hexagonal-layout]].
3. Same layer, right behind it: the generic `IParse`-keyed JSON/YAML converters for identifiers and composites (one converter family for all value types — wire-capability on the type, wire format in the adapter, per the Parse boundary rule in [[domain-language]]). Unblocks the REST hosts. Tracked in [[move-jsonconverter-off-identifier-types-into-the-json-adapter]].
4. Later, unblocked by any of the above: the rewrite interpreters (Check's boolean walk, Expand's tree) with statement lookup as a port, per [[four-service-split-by-load-profile]].
