---
title: Dissolve Schema into administration
summary: "Execute [[schema-dissolves-into-administration]]: Namespace becomes the config aggregate root, Schema/SchemaIdentifier/SchemaVersion retire, the kookie becomes the one pin, namespaces get dots, and the corpus reframes."
tags: [todo, schemas, evaluation]
created: 2026-07-21
priority: high
effort: high
status: open
---

# Dissolve Schema into administration

Execute the remodel [[schema-dissolves-into-administration]] decided. The rewrite-interpreters clean room ([[rewrite-interpreters]]) designs against the post-dissolution model; this todo carries the mechanical and corpus work.

## Domain code

- Re-promote `Namespace` (`src/Kingo.Schemas/Namespace.cs`) to config aggregate root; it is the stored, versioned artifact. Its `Create` validation stands unchanged.
- Retire `Schema` (`src/Kingo.Schemas/Schema.cs`), `SchemaIdentifier` (`src/Kingo/SchemaIdentifier.cs`), and `SchemaVersion` (`src/Kingo/SchemaVersion.cs`). `schema.empty` and `schema.duplicate_namespace` retire with the class; global namespace uniqueness and SDL-push atomicity become Write-against-store guarantees.
- Add `.` to the `<namespace>` character rule (`src/Kingo/NamespaceIdentifier.cs` and the [[domain-language]] terminals table) so dotted naming conventions (`youtube.group`) can carry hierarchy. Namespaces don't currently allow dots.
- `Kingo.Sdl`: the envelope goes — `schema:` and `namespaces:` keys retire (decided 2026-07-21). Re-ruled 2026-07-22: the parse target is a single namespace body, the relationship list alone (`- owner`, `- viewer: (this | (parent, viewer)) ! banned`); the namespace name arrives out of band and the parse exits through `Namespace.Create`. `sdl.document` and `schema_id.*` error codes retire or reshape. Edit permission is a per-namespace check (prefix rules against caller identity). Administrative grouping/ownership is modelable as facts in an admin namespace — a `Contains` call, no domain type.
- Config mutation is per-namespace config pushes, re-ruled 2026-07-22, superseding the 2026-07-21 documents-only ruling. The flyway-style script language was designed to a working grammar, then discarded: its identity, run-history, and series-ordering machinery served non-idempotent scripts, and the documents had come out idempotent upserts. Zanzibar's own model replaces it. The Write API takes `PUT /namespaces/{name}` with the namespace body as the YAML payload (idempotent upsert, create-or-replace) and `DELETE /namespaces/{name}`, refused while live facts reference the namespace. Absence still means nothing; deletion is always the explicit verb, never inferred from a missing file. Git-storability survives because the wire body is the stored artifact: a repo directory holds one `<namespace>.yaml` per namespace, filename carrying the name and content the body, and deploy is a loop of PUTs. A block push also exists (ruled 2026-07-22): one document, root key `namespaces:`, holding a map of namespace name to body, upserted as a single atomic batch. The block carries applies only, never drops — dropping a namespace is always the explicit `DELETE` call (ruled 2026-07-22). Endpoint spelling for the block is open. A rewrite may reference a namespace that does not exist yet (accepted 2026-07-22): error-taxonomy family 1 fails closed on undefined references at evaluation, so write-time tolerance adds no hazard. Storage delta for [[storage-versioning-design]]: no changelog of applied scripts; each push versions on the store timeline and the [[kookie]] pins config state; version states materialize as per-namespace SDL blobs. [[sdl-becomes-a-script-language]] is superseded by this ruling; bulk fact documents stay [[graph-document-is-bulk-dml]]'s territory.

## Corpus

- [[domain-language]]: revert the config side of the aggregate table; update the envelope note and terminals table.
- [[schema-definition-language]]: reframe the envelope (`schema:` as ownership label); keep the namespace map.
- [[drift-prevention-at-the-write-edges]]: amend the coherent (`Kookie`, `SchemaVersion`) pair to the one-pin model — namespace configs version on the store timeline and the kookie pins both facts and config.
- [[storage-versioning-design]]: the versioned config artifact is the namespace; whole-namespace deletion is the terminal marker; schema-version encoding questions retire.
- Glossary: `schema` reframes to the authoring-context sense (SQL's DDL sense: the body of namespace definitions an SDL document declares; also the administrative grouping word). No domain type behind it; the evaluation context's word is `catalog`, the config aggregate is `namespace`.

## Blast radius (mapped 2026-07-21)

Code — retire or reshape:

- `src/Kingo/SchemaIdentifier.cs`, `src/Kingo/SchemaVersion.cs` — retire.
- `src/Kingo.Schemas/Schema.cs` — retire; `Namespace.cs` promotes to root; `Relationship.cs`, `SubjectSetRewrites.cs` unchanged (doc comments mention schema).
- `src/Kingo.Sdl/SchemaParser.cs`, `SchemaPrinter.cs` — reshape to the single-namespace body (parse target: one relationship list, name supplied out of band); `RewriteExpressionParser/Printer` unchanged.
- `src/Kingo.Closures/Decision.cs`, `Expansion.cs` — drop the schema-version slot: four slots, the kookie is the one pin (facts and config on one timeline).
- Project names: `Kingo.Schemas` may rename (`Kingo.Namespaces`?) — namespaces are the aggregate; decide during execution.

Tests — follow their subjects:

- `tests/Kingo.Tests/SchemaIdentifierTests.cs`, `tests/Kingo.Schemas.Tests/SchemaTests.cs` — retire with their types.
- `tests/Kingo.Sdl.Tests/SchemaParseTests.cs`, `SchemaPrinterTests.cs`, `SchemaRoundTripTests.cs`, `TestHelpers.cs` — reshape to the new document.
- Architecture tests in `Kingo.Schemas.Tests`/`Kingo.Graphs.Tests` — verify the cross-reference bans still hold under any rename.

Corpus — reframe (journals stay immutable; open todos re-ruled where they assume Schema):

- Heavy: [[domain-language]] (aggregate table, envelope, terminals), [[schema-definition-language]] (whole envelope section), [[drift-prevention-at-the-write-edges]] (pair → one pin), [[storage-versioning-design]] (versioned artifact is the namespace; SchemaVersion encoding questions retire), [[rewrite-interpreters]] (SchemaVersion/Schema references throughout — the clean room's specs supersede as they land).
- Light: [[dynamodblite-substrate]] (schema-storage rows), [[four-service-split-by-load-profile]], [[architecture]], [[authz-event-logging]], [[immutablearray-for-domain-collections]], [[namespace-create-validation]], [[graph-document-is-bulk-dml]], [[realign-serialization-projects-around-their-real-consumers]].
- Glossary: [[schema]] (reframe to the authoring/administrative senses), [[namespace]] (now the config root), [[decision]]/[[expansion]] (four slots), [[closure]], [[fail-closed]], README index line.
