---
title: Realign serialization projects around their real consumers
summary: "Mark's post-review correction: .Json/.Yaml exist purely as value-type converter packs for future ASP.NET REST hosts — no document ever crosses the wire — so the IDocumentSerializer port and Kingo.Serialization dissolved; final SDL surface is SdlParser.Parse(text) → Result<Schema> plus the schema.Print() extension."
tags: [note, todo, hexagonal, serialization, sdl]
created: 2026-07-14
status: closed
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

The adapter side, as landed 2026-07-15: `SdlParser.Parse(text) → Result<Schema>` (calls `Schema.Create` as its last step — the former `RequireUniqueNames` folded into the domain) plus a `schema.Print()` extension in the adapter (`SdlPrinter`) — format knowledge stays adapter-side while the call site reads as a domain capability. The interim `SdlDocument : IParse<SdlDocument>` wrapper idea was dropped: with `Parse` returning the domain value directly there is no wrapper left to justify. `Print` takes `Schema`, so the duplicate-namespace throw deleted itself (unrepresentable by construction); the reserved-word `ArgumentException` remains the one caller-defect (the core allows `this`/`...` as relationship names; SDL cannot express them). The old quarry's `PdlDocument(string Yaml, ImmutableArray<Namespace> Namespaces)` pointed at the concept but the domain half became `Schema` itself.

- Cost accepted: no instance-level format substitution (callers bind statically) — runtime format choice is exactly the scenario that will never happen.
- `AdapterArchitectureTestsBase` lost its port anchor ("public adapter types implement a port" rule removed with the port). Decided 2026-07-15: replaced with **nothing**. `SdlParser`/`SdlPrinter` are static pure entry points — no port exists to anchor the rule, and faking a pure parse buys nothing a canned `Schema.Create` doesn't. The interface rule returns when the first genuine port family (storage) arrives; the fake-ability Mark wants lands there (e.g. an `ISchemaSource.Load() → Result<Schema>` host port whose adapter composes I/O + `SdlParser`). Only `NoExceptionTypesAreDefined` remains in the base.

## Resolution

- ~~Dissolve `Kingo.Serialization`~~ — done 2026-07-14: project + tests deleted, references replaced (`.Pdl` → Kingo + Results; `.Json`/`.Yaml` → Kingo), removed from `Kingo.slnx`, `SdlSerializer` detached from the interface, `PublicTypesImplementAPort` rule and `portAssemblyName` removed from `AdapterArchitectureTestsBase`. Build/tests deliberately not run yet.
- ~~Add `Schema`~~ — done 2026-07-14 (`Kingo.Schemas.Schema`; `Create` is the only construction path (private ctor, house Cons): `schema.empty`, `schema.duplicate_namespace`; SchemaTests in Kingo.Schemas.Tests).
- ~~Update [[architecture]]~~ — done 2026-07-14 (ports section rewritten; serialization-project jobs corrected).
- ~~Rework `Kingo.Sdl` public surface~~ — done 2026-07-15: `SdlParser.Parse(text) → Result<Schema>`, `schema.Print()` extension, `RequireUniqueNames` folded into `Schema.Create`, `SdlDocument` plan dropped.
- ~~Rename `SdlSerializer` → `SdlParser`~~ — done 2026-07-15, with `RewriteExpressionRenderer` → `RewriteExpressionPrinter` (the compiler-lineage parser/printer pair; the round-trip tests pin parse ∘ print = id).
- ~~Reframe [[move-jsonconverter-off-identifier-types-into-the-json-adapter]] wording~~ — done 2026-07-14 in that note's body: the converter packs are the whole point of the .Json/.Yaml projects; nothing implements or hangs off a port.
