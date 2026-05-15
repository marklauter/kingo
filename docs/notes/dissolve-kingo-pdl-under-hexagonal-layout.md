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
Split the contents:
- AST records + identifiers + `RegExPatterns` + `PolicyHash` → `Kingo` (domain core).
- `PdlParser`, `PdlSerializer`, `RewriteExpressionParser`, `Converters/*`, `PdlParseException` → new `Kingo.Serialization.Pdl` adapter (paired with `Kingo.Serialization.Pdl.Tests`).
- Move tests: domain tests → `Kingo.Tests`; parser/serializer tests → `Kingo.Serialization.Pdl.Tests`.
- Update `Kingo.Pdl.Tests/Architecture/ArchitectureTests.cs` ArchUnit rules: rescope to the new locations (domain rules under `Kingo`, adapter rules under `Kingo.Serialization.Pdl`).
- Delete `Kingo.Pdl` and `Kingo.Pdl.Tests` from `Kingo.slnx` once empty.

Likely coordinated with [move-jsonconverter-off-identifier-types-into-the-json-adapter](move-jsonconverter-off-identifier-types-into-the-json-adapter.md) and [ivalue-tself-tvalue-absorbs-all-value-type-wrappers](ivalue-tself-tvalue-absorbs-all-value-type-wrappers.md).
