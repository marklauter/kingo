---
type: todo
title: Realign serialization projects around their real consumers
summary: "Mark's post-review correction: .Json/.Yaml exist purely as value-type converter packs for future ASP.NET REST hosts — no document ever crosses the wire — so IDocumentSerializer has one consumer and the shared port (and possibly Kingo.Serialization itself) should go; candidate replacement is PdlDocument : IParse<PdlDocument>."
tags: [note, todo, hexagonal, serialization, pdl]
created: 2026-07-14
status: open
priority: medium
effort: low
---

# Realign serialization projects around their real consumers

## Observation

The PDL adapter slice ([[dissolve-kingo-pdl-under-hexagonal-layout]]) ran unsupervised and generalized too early. Mark's corrections (2026-07-14 review):

- `Kingo.Serialization.Json` / `.Yaml` are **strictly converter packs**: converters for the `Kingo` value types (and any other types that cross the wire), existing purely so future ASP.NET REST hosts can function. The system will **never accept a full namespace or PDL document over the wire as JSON/YAML** — so those projects would never implement or use `IDocumentSerializer<T>` for any reason.
- That leaves `IDocumentSerializer<T>` with exactly one implementation, ever: PDL. A shared port with one possible adapter isn't a port; it's ceremony.
- `Kingo.Serialization` (the ports project) has no reason to exist (Mark, firmly): its entire content is `IDocumentSerializer.cs`; `IParse` lives in Values; converter packs need no shared interface; a future genuine port family gets its own project (`Kingo.Storage`), not a grab-bag.

## Candidate direction

Settled 2026-07-14 (Mark + tour discussion): the domain concept behind "PDL document" is **`Policy`** — `Kingo.Policies.Policy` (the config-side aggregate root — see [[domain-language]]; the C# namespace renamed `Namespaces` → `Policies` with the root swap), a value over `ImmutableArray<Namespace>`, non-empty (an empty policy is the absence of a policy) with unique namespace names; `Policy.Create` enforces both (now implemented). `Statement` stays `Statement` — a ground fact is what policy ranges over, not policy itself (rules are intensional, statements extensional; the tell: delete the rewrite rule and the edge governs nothing).

The adapter side: `PdlDocument : IParse<PdlDocument>` — parses itself from PDL text (CRTP fits because the document type is format-specific; its canonical text form genuinely is PDL), calls `Policy.Create` as its last step, and exposes the domain value as a trusted projection (`Policy` property / `ToPolicy()`, total, no Result). The old quarry's `PdlDocument(string Yaml, ImmutableArray<Namespace> Namespaces)` had the right shape but no domain half; its hash helper was already named `PolicyHash`.

- Rendering: `ToString()` gives the canonical PDL text — same Parse/ToString round-trip idiom as every domain value. Validation at construction (reserved relationship names rejected at `Parse`/construction) makes rendering total — removes the wart where `Serialize` threw `ArgumentException` on reserved names.
- Cost: no instance-level format substitution (callers bind statically to `PdlDocument.Parse`) — acceptable because runtime format choice is exactly the scenario that will never happen.
- `AdapterArchitectureTestsBase` lost its port anchor ("public adapter types implement a port" rule removed with the port); needs a replacement convention if the adapter arch rules survive the realignment.

## Next

- ~~Dissolve `Kingo.Serialization`~~ — done 2026-07-14: project + tests deleted, references replaced (`.Pdl` → Kingo + Results; `.Json`/`.Yaml` → Kingo), removed from `Kingo.slnx`, `PdlSerializer` detached from the interface, `PublicTypesImplementAPort` rule and `portAssemblyName` removed from `AdapterArchitectureTestsBase`. Build/tests deliberately not run yet.
- ~~Add `Policy`~~ — done 2026-07-14 (`Kingo.Policies.Policy`; `Create` is the only construction path (private ctor, house Cons): `policy.empty`, `policy.duplicate_namespace`; PolicyTests in Kingo.Tests).
- ~~Update [[architecture]]~~ — done 2026-07-14 (ports section rewritten; serialization-project jobs corrected).
- Rework `Kingo.Serialization.Pdl` public surface: `PdlSerializer` → `PdlDocument : IParse<PdlDocument>` + `ToString()`, projecting `Policy`; fold the deserializer's `RequireUniqueNames` into `Policy.Create`.
- Reframe [[move-jsonconverter-off-identifier-types-into-the-json-adapter]] wording: converters don't "implement or hang off ports"; the converter packs are the whole point of those projects.
