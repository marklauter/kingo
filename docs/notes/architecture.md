---
title: Architecture
summary: "Kingo organizes as hexagonal with a DDD core: Kingo holds the domain (Schemas and Graphs), Kingo.Sdl is the spec-document codec, .Json/.Yaml are wire-converter packs; no ports project until a genuine port family appears."
tags: [note, architecture, hexagonal, ddd]
created: 2026-05-13
status: evolving
---

# Architecture

## Organization

The project follows hexagonal architecture with a DDD core at the center. Projects layer outward from pure domain to concrete I/O.

### Domain core — `Kingo`

The center. Pure types describing the ubiquitous domain per [[domain-language]]: the identifier IValues (cross-cutting vocabulary at root `Kingo`), and one plural C# namespace per aggregate root — `Schemas` (the config side: the `Spec` root over its `Namespace` entities, plus the `SubjectSetRewrite` algebra — parse-agnostic, deliberately not an AST) and `Graphs` (the data side: the `Fact` root with its value objects `Resource` and `SubjectSet`; the party seats as `SubjectId` directly — [[resource-fact-case]] dissolved the `Subject` wrapper, 2026-07-21). No knowledge of how anything is persisted, serialized, transported, rendered, or authenticated.

The foundational primitives — `Result<T>` / `Error` (Results project) and `IValue<TSelf, TValue>` / `IParse<TSelf>` / `ITryParse<TSelf>` (Values project) — sit *below* the domain core as separate assemblies; `Kingo` consumes them. The legacy `Kingo.Pdl` quarry was dissolved and deleted per [[dissolve-kingo-pdl-under-hexagonal-layout]]; it survives only on the archive branches ([[sources]]).

### Ports — future `Kingo.Storage`, etc.

Interfaces that describe what the core needs from the outside world, without specifying how. A port says "give me something that can store a fact"; it does not say "give me a DynamoDB client." Ports live close enough to the core that they share its language; the implementations live elsewhere.

No ports project exists today. The first attempt — `Kingo.Serialization` holding `IDocumentSerializer<T>` — was dissolved 2026-07-14 ([[realign-serialization-projects-around-their-real-consumers]]): SDL was its only possible consumer, so it was ceremony, not a port. When a genuine port family appears (storage, transport), it gets its own project. What survives from that slice: `Deserialize`/`Parse` at a trust boundary returns `Result<T>` with accumulated errors, never exceptions, and `AdapterArchitectureTestsBase` still enforces that an adapter defines no exception types.

That trigger has now fired, though the project has not been built yet. The bulk-DML graph document ([[graph-document-is-bulk-dml]], 2026-07-15) needs a `GraphOperation` vocabulary that belongs to neither side of the current split: it cannot live in core (every rule it carries — create-vs-touch, delete-of-absent, atomicity — is storage semantics, not a domain invariant), it cannot live in `Kingo.Sdl` (the Write host would depend on a YAML adapter to speak its own commands), and it cannot live in a host (adapters would depend upward). Unlike `IDocumentSerializer`, the write port it belongs to has real alternative adapters — DynamoDbLite, DynamoDB, an in-memory fake — so it is a port rather than ceremony. It lands with the storage work ([[storage-versioning-design]], [[dynamodblite-substrate]]), and the "public adapter types implement a port" ArchUnit rule returns with it.

### Adapters — `Kingo.Serialization.Json`, `Kingo.Serialization.Yaml`, `Kingo.Sdl`, future storage adapters, transport adapters, etc.

Concrete implementations of the ports, using whichever third-party library or platform is appropriate. Adapters know about YamlDotNet, System.Text.Json, DynamoDbLite, ASP.NET Core. Domain code never directly references them; it talks to the port.

The serialization projects have distinct jobs ([[realign-serialization-projects-around-their-real-consumers]]): `Kingo.Sdl` is the whole-document codec (YamlDotNet + Superpower; public surface: `SpecParser.Parse(text) → Result<Spec>` and the `spec.Print()` extension — format knowledge in the adapter, domain untouched; the fact-side document has no adapter yet: its stubs were deleted 2026-07-15 because that document is a bulk-DML changeset, not a state, and its `GraphOperation` vocabulary belongs between the edges rather than in core — see [[graph-document-is-bulk-dml]], which waits on the first ports project); `.Json` and `.Yaml` are strictly converter packs for the `Kingo` value types so future ASP.NET REST hosts can function — no document ever crosses the wire ([[move-jsonconverter-off-identifier-types-into-the-json-adapter]]).

## Principles

- **The domain doesn't know how it's stored.** No serialization attributes, ORM annotations, or framework references on domain types. The mapping happens at the adapter boundary.
- **Ports speak the domain's language.** A port interface uses domain types as parameters and returns; it does not leak adapter-specific concepts (no `JsonElement`, no `YamlNode`) into the core.
- **Adapters are swappable in principle.** The system should function with any conforming implementation of a port. In practice this is most visible at test time, where in-memory or fake adapters substitute for the production ones.
- **First slice sets the layer.** When a new layer appears (the first storage adapter, the first transport adapter), the structural rules for that layer are encoded immediately — naming, project shape, ArchUnit rules. The first example teaches by existing.

## Influences

- Evans, *Domain-Driven Design* — the ubiquitous language, the model as the heart of the application.
- Cockburn, "Hexagonal Architecture" — ports and adapters as the substrate-independence pattern.
- Writing-csharp principles — particularly "The domain doesn't know how it's stored" and "Make invalid states unrepresentable."

## Open threads

- [[dissolve-kingo-pdl-under-hexagonal-layout]]
- [[move-jsonconverter-off-identifier-types-into-the-json-adapter]]
- [[realign-serialization-projects-around-their-real-consumers]] — `Kingo.Serialization` dissolved 2026-07-14; SDL public-surface rework landed 2026-07-15: `SpecParser.Parse` + `spec.Print()`; the interim `SdlDocument : IParse<SdlDocument>` idea was dropped — no wrapper type needed.
