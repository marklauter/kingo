# Dissolve Kingo.Pdl under hexagonal layout

Tags: todo,hexagonal
AST belongs in Kingo (domain core); parser+serializer belong in a serialization adapter; the standalone Kingo.Pdl project disappears.

## Observation
Kingo.Pdl currently contains:
- AST records: `PdlDocument`, `Namespace`, `Relation`, `SubjectSetRewrite` (abstract) and its sealed inhabitants (`ThisRewrite`, `ComputedSubjectSetRewrite`, `TupleToSubjectSetRewrite`, `UnionRewrite`, `IntersectionRewrite`, `ExclusionRewrite`).
- Value-type wrappers: `NamespaceIdentifier`, `RelationIdentifier`.
- Helpers: `PolicyHash`, `RegExPatterns`.
- Parser/serializer: `PdlParser` (YAML via YamlDotNet), `PdlSerializer`, `RewriteExpressionParser` (Superpower), `Converters/RelationTypeConverter`, `Converters/YamlStringConvertible`, `PdlParseException`.

Project organization established for `reboot`: domain types live in `Kingo` (the core); ports in `Kingo.Serialization`; adapters in `Kingo.Serialization.Json` / `Kingo.Serialization.Yaml`.

## Interpretation
Under hexagonal layering, `Kingo.Pdl` as a standalone project has no clean home — its content is half domain (the AST and identifiers) and half adapter (the parser and serializer). Keeping it as a single project would either drag domain types into adapter territory or drag adapter code into domain territory.

## Next
Split the contents:
- AST records + identifiers + `RegExPatterns` + `PolicyHash` → `Kingo` (domain core).
- `PdlParser`, `PdlSerializer`, `RewriteExpressionParser`, `Converters/*`, `PdlParseException` → `Kingo.Serialization.Yaml` *or* a dedicated `Kingo.Serialization.Pdl` adapter if PDL deserves its own format module distinct from generic YAML. Decide at execution time.
- Move tests: domain tests → `Kingo.Tests`; parser/serializer tests → the adapter's test project.
- Update `Kingo.Pdl.Tests/Architecture/ArchitectureTests.cs` ArchUnit rules to scope to new locations (and rename the project accordingly).
- Delete `Kingo.Pdl` and `Kingo.Pdl.Tests` from `Kingo.slnx` once empty.

Likely coordinated with [move-jsonconverter-off-identifier-types-into-the-json-adapter](move-jsonconverter-off-identifier-types-into-the-json-adapter.md) and the planned identifier rewrite (drops `IStringConvertible<T>`, adopts `IValue<TSelf, TValue>`).
