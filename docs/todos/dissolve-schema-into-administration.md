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
- `Kingo.Sdl`: the envelope goes — `schema:` and `namespaces:` keys retire and the document root becomes the namespace map (decided 2026-07-21). The parse target is the namespace set, exiting through `Namespace.Create` per entry; `sdl.document` and `schema_id.*` error codes retire or reshape. A document push is one Write transaction with per-namespace edit-permission checks (prefix rules against caller identity); any unauthorized namespace rejects the whole document. Administrative grouping/ownership is modelable as facts in an admin namespace — a `Contains` call, no domain type.
- Config mutation is documents-only (decided 2026-07-21): the Write API takes SDL documents, never verb endpoints, so every mutation is git-storable and flyway-sequenceable. Document semantics are idempotent upsert per named namespace (absence means nothing — implicit delete is dead; the drift ceremony already refused it). Deletion becomes explicit SDL grammar (a `drop` marker of some spelling), refused while live facts reference the namespace; migrations are ordered document sequences (fact migration before the drop lands). Grammar design — marker spelling, reserved words, operations-document kind, FML/SDL sequencing, series ordering — is [[sdl-becomes-a-script-language]]'s work, not this todo's. Storage delta for [[storage-versioning-design]]: changelog entries record applied scripts, not whole-schema snapshots; version states materialize as per-namespace SDL blobs.

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
- `src/Kingo.Sdl/SchemaParser.cs`, `SchemaPrinter.cs` — reshape to the envelope-free document (parse target: namespace set); `RewriteExpressionParser/Printer` unchanged.
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
