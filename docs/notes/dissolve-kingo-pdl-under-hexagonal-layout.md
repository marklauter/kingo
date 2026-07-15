---
type: todo
title: Dissolve Kingo.Pdl under hexagonal layout
summary: "Closed 2026-07-14: the domain half landed by fresh construction in Kingo core; the adapter half landed as Kingo.Serialization.Pdl (first port interface, adapter-layer ArchUnit rules, Result-first parser/serializer); the Kingo.Pdl quarry is deleted."
tags: [note, todo, hexagonal, pdl]
created: 2026-05-13
status: closed
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

**Update 2026-07-14 — closed.** The adapter landed as planned, with the shape it sets recorded in [[architecture]]:

- `IDocumentSerializer<T>` in `Kingo.Serialization` is the first port: `Serialize(T) → string` (total for valid domain input) and `Deserialize(string) → Result<T>` (the trust boundary).
- `PdlSerializer : IDocumentSerializer<ImmutableArray<Namespace>>` is the only public type in `Kingo.Serialization.Pdl`. The Superpower grammar produces an internal string-leaf AST (`RewriteNode`) that transforms into the core algebra at its exit through `RelationshipIdentifier.Parse` / `Namespace.Define`, accumulating every error; the YAML walk uses the representation model, catching only `YamlException` at the load boundary and translating it to a `pdl.syntax` validation error. Serializer newline pinned via `WithNewLine("\n")`.
- Two quarry bugs fixed in the rewrite: the operator-chain fold flattens only *consecutive same-operator runs*, so a parenthesized operand stays opaque and nested trees round-trip structurally; and the renderer parenthesizes by grammar position (compound exclude-side terms included), so arbitrary domain trees — not just parser-produced ones — reparse to structurally equal trees.
- `AdapterArchitectureTestsBase` (Kingo.Testing) encodes the adapter-layer rules — public types implement a port; no exception types defined — and the `.Json`/`.Yaml` arch tests already derive from it.
- `Kingo.Pdl` and `Kingo.Pdl.Tests` deleted; the quarry survives only on the archive branches ([[sources]]). Gate green, adapter at 100% line/branch/method.
