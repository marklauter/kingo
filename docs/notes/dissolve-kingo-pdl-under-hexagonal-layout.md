---
type: todo
title: Dissolve Kingo.Pdl under hexagonal layout
summary: "The domain half landed by fresh construction in Kingo core; what remains is rewriting the parser/serializer as the Kingo.Serialization.Pdl adapter and deleting the Kingo.Pdl quarry project. Unblocked 2026-07-14 — the active work item."
tags: [note, todo, hexagonal, pdl]
created: 2026-05-13
status: open
priority: high
effort: medium
---

# Dissolve Kingo.Pdl under hexagonal layout

## Observation

Kingo.Pdl currently contains:

- AST records: `PdlDocument`, `Namespace`, `Relation`, `SubjectSetRewrite` (abstract) and its sealed inhabitants (`ThisRewrite`, `ComputedSubjectSetRewrite`, `TupleToSubjectSetRewrite`, `UnionRewrite`, `IntersectionRewrite`, `ExclusionRewrite`).
- Value-type wrappers: `NamespaceIdentifier`, `RelationIdentifier` (now implementing `IValue<TSelf, string>` additively).
- Helpers: `PolicyHash`, `RegExPatterns`.
- Parser/serializer: `PdlParser` (YAML via YamlDotNet), `PdlSerializer`, `RewriteExpressionParser` (Superpower), `Converters/RelationTypeConverter`, `Converters/YamlStringConvertible`, `PdlParseException`.

Project organization established for `reboot`: domain types live in `Kingo` (the core); ports in `Kingo.Serialization`; adapters in `Kingo.Serialization.Json` / `Kingo.Serialization.Yaml`.

## Interpretation

Under hexagonal layering, `Kingo.Pdl` as a standalone project has no clean home — its content is half domain (the AST and identifiers) and half adapter (the parser and serializer). Keeping it as a single project would either drag domain types into adapter territory or drag adapter code into domain territory.

PDL is a distinct format — YAML outer structure plus a Superpower-parsed embedded mini-language for rewrite expressions. That's not "general YAML"; it's its own grammar. A dedicated `Kingo.Serialization.Pdl` adapter sits alongside `.Json` and `.Yaml` as a peer rather than folding PDL's specifics into the generic YAML adapter.

## Next

**Update 2026-07-14: the domain half of this dissolve happened by fresh construction, not by moving files.** `Kingo` core was written new per [[domain-language]]: identifier IValues (with case normalization and per-terminal patterns classes), the grammar compositions, `Statement`, and the policy model (`Namespace`, `Relationship`, the `SubjectSetRewrite` algebra — deliberately *not* called an AST; it's parse-agnostic). `Kingo.Pdl` was never migrated and no longer builds; it is now purely quarry.

Remaining work:

- Rewrite the parser/serializer as `Kingo.Serialization.Pdl`, targeting the new core types (`Namespace`, `Relationship`, `SubjectSetRewrite`). The parser's own AST, if it needs one, stays `internal` to the adapter and transforms into core types at its exit — parse errors surface as `Result` failures, not `PdlParseException`.
- Salvage from `Kingo.Pdl` as reference: `PdlParser`/`PdlSerializer` structure, `RewriteExpressionParser` (Superpower grammar — matches the BNF in [[pdl-yaml]]), round-trip tests. Two review findings from the quarry carry forward as requirements: no catch-all exception wrapping at the parse boundary (the `Result`-failure mandate above covers it), and round-trip tests must not assert platform line endings byte-exact — CI runs `ubuntu-latest`, so pin the serializer's newline (`.WithNewLine("\n")`) or normalize before asserting.
- New `Kingo.Serialization.Pdl.Tests` with ArchUnit rules for the adapter layer.
- Delete `Kingo.Pdl` (and its tests) once the adapter round-trips.

Likely coordinated with [[move-jsonconverter-off-identifier-types-into-the-json-adapter]].

**Update 2026-07-14 — unblocked; this is the active work item.** The core test pass closed the same day (ten test files pin the identifier grammars, delimiter reservations, composite Parse/ToString round-trips, applicative error accumulation, and the ImmutableArray structural-equality overrides; `build-gate.sh` green, Kingo at 98% line / 100% branch). Additions to the plan since it was written:

- **The transform exits through `Namespace.Define`, not the raw constructor.** `Define(name, relationships)` is the core's `Result`-returning structured factory (landed with the test pass): duplicate relationship names fail as accumulated `namespace.duplicate_relationship` validation errors. The adapter decodes the document, then calls `Define` at the untrusted boundary — its first real caller. The raw constructor is pure assignment for trusted sources only, mirroring the `Create`/`Parse` split ([[domain-language]]).
- **First slice sets the layer.** This work creates the first port interface in `Kingo.Serialization` and the adapter-layer ArchUnit rules; `.Json`/`.Yaml` inherit the shape. The three serialization projects are scaffolded but empty today.
- **Queue behind it:** [[move-jsonconverter-off-identifier-types-into-the-json-adapter]] (unblocks REST hosts), then in any order the rewrite interpreters ([[four-service-split-by-load-profile]]), storage on DynamoDbLite ([[dynamodblite-substrate]]), and the zookie/snapshot design session — the Write host waits on all three.
