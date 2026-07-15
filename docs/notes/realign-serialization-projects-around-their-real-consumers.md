---
type: todo
title: Realign serialization projects around their real consumers
summary: "Mark's post-review correction: .Json/.Yaml exist purely as value-type converter packs for future ASP.NET REST hosts — no document ever crosses the wire — so IDocumentSerializer has one consumer and the shared port (and possibly Kingo.Serialization itself) should go; candidate replacement is SdlDocument : IParse<SdlDocument>."
tags: [note, todo, hexagonal, serialization, pdl]
created: 2026-07-14
status: open
priority: medium
effort: low
---

# Realign serialization projects around their real consumers

## Observation

The SDL adapter slice ([[dissolve-kingo-pdl-under-hexagonal-layout]]) ran unsupervised and generalized too early. Mark's corrections (2026-07-14 review):

- `Kingo.Serialization.Json` / `.Yaml` are **strictly converter packs**: converters for the `Kingo` value types (and any other types that cross the wire), existing purely so future ASP.NET REST hosts can function. The system will **never accept a full namespace or SDL document over the wire as JSON/YAML** — so those projects would never implement or use `IDocumentSerializer<T>` for any reason.
- That leaves `IDocumentSerializer<T>` with exactly one implementation, ever: SDL. A shared port with one possible adapter isn't a port; it's ceremony.
- `Kingo.Serialization` (the ports project) has no reason to exist (Mark, firmly): its entire content is `IDocumentSerializer.cs`; `IParse` lives in Values; converter packs need no shared interface; a future genuine port family gets its own project (`Kingo.Storage`), not a grab-bag.

## Candidate direction

Settled 2026-07-14 (Mark + tour discussion): the domain concept behind "SDL document" is **`Schema`** — `Kingo.Schemas.Schema` (the config-side aggregate root — see [[domain-language]]; the C# namespace renamed `Namespaces` → `Schemas` with the root swap), a value over `ImmutableArray<Namespace>`, non-empty (an empty schema is the absence of a schema) with unique namespace names; `Schema.Create` enforces both (now implemented). The stored triple keeps its own name — a ground fact is what rules range over, not a rule itself (rules are intensional, facts extensional; the tell: delete the rewrite rule and the edge governs nothing) — first as `Statement`, renamed `Fact` 2026-07-15 with the `Policy` → `Schema` rename (naming rationale: [[domain-language]]).

The adapter side: `SdlDocument : IParse<SdlDocument>` — parses itself from SDL text (CRTP fits because the document type is format-specific; its canonical text form genuinely is SDL), calls `Schema.Create` as its last step, and exposes the domain value as a trusted projection (`Schema` property / `ToSchema()`, total, no Result). The old quarry's `SdlDocument(string Yaml, ImmutableArray<Namespace> Namespaces)` had the right shape but no domain half; its hash helper was already named `PolicyHash`.

- Rendering: `ToString()` gives the canonical SDL text — same Parse/ToString round-trip idiom as every domain value. Validation at construction (reserved relationship names rejected at `Parse`/construction) makes rendering total — removes the wart where `Serialize` threw `ArgumentException` on reserved names.
- Cost: no instance-level format substitution (callers bind statically to `SdlDocument.Parse`) — acceptable because runtime format choice is exactly the scenario that will never happen.
- `AdapterArchitectureTestsBase` lost its port anchor ("public adapter types implement a port" rule removed with the port); needs a replacement convention if the adapter arch rules survive the realignment.

## Next

- ~~Dissolve `Kingo.Serialization`~~ — done 2026-07-14: project + tests deleted, references replaced (`.Pdl` → Kingo + Results; `.Json`/`.Yaml` → Kingo), removed from `Kingo.slnx`, `SdlSerializer` detached from the interface, `PublicTypesImplementAPort` rule and `portAssemblyName` removed from `AdapterArchitectureTestsBase`. Build/tests deliberately not run yet.
- ~~Add `Schema`~~ — done 2026-07-14 (`Kingo.Schemas.Schema`; `Create` is the only construction path (private ctor, house Cons): `schema.empty`, `schema.duplicate_namespace`; SchemaTests in Kingo.Tests).
- ~~Update [[architecture]]~~ — done 2026-07-14 (ports section rewritten; serialization-project jobs corrected).
- Rework `Kingo.Serialization.Sdl` public surface: `SdlSerializer` → `SdlDocument : IParse<SdlDocument>` + `ToString()`, projecting `Schema`; fold the deserializer's `RequireUniqueNames` into `Schema.Create`.
- Reframe [[move-jsonconverter-off-identifier-types-into-the-json-adapter]] wording: converters don't "implement or hang off ports"; the converter packs are the whole point of those projects.
