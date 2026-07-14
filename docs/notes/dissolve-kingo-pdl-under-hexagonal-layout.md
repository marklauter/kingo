# Dissolve Kingo.Pdl under hexagonal layout

Tags: todo,hexagonal
AST and identifiers belong in Kingo (domain core); parser+serializer move to a new Kingo.Serialization.Pdl adapter; the standalone Kingo.Pdl project disappears.

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

**Update 2026-07-14: the domain half of this dissolve happened by fresh construction, not by moving files.** `Kingo` core was written new per [domain-language](domain-language.md): identifier IValues (with case normalization and per-terminal patterns classes), the grammar compositions, `Statement`, and the policy model (`Namespace`, `Relationship`, the `SubjectSetRewrite` algebra — deliberately *not* called an AST; it's parse-agnostic). `Kingo.Pdl` was never migrated and no longer builds; it is now purely quarry.

Remaining work:
- Rewrite the parser/serializer as `Kingo.Serialization.Pdl`, targeting the new core types (`Namespace`, `Relationship`, `SubjectSetRewrite`). The parser's own AST, if it needs one, stays `internal` to the adapter and transforms into core types at its exit — parse errors surface as `Result` failures, not `PdlParseException`.
- Salvage from `Kingo.Pdl` as reference: `PdlParser`/`PdlSerializer` structure, `RewriteExpressionParser` (Superpower grammar — matches the BNF in [pdl-yaml](pdl-yaml.md)), round-trip tests.
- New `Kingo.Serialization.Pdl.Tests` with ArchUnit rules for the adapter layer.
- Delete `Kingo.Pdl` (and its tests) once the adapter round-trips.

Likely coordinated with [move-jsonconverter-off-identifier-types-into-the-json-adapter](move-jsonconverter-off-identifier-types-into-the-json-adapter.md) and [ivalue-tself-tvalue-absorbs-all-value-type-wrappers](ivalue-tself-tvalue-absorbs-all-value-type-wrappers.md).
