---
title: Architecture
summary: "Hexagonal with a DDD core: Kingo holds the identifiers, Kingo.Facts and Kingo.Theories hold the two halves of the model, Kingo.Closures is where they meet, and adapters own every text format."
tags: [note, architecture, hexagonal, ddd]
created: 2026-05-13
status: evolving
---

# Architecture

Hexagonal, with a DDD core at the center. Projects layer outward from pure domain to concrete I/O, and the dependency graph is acyclic in that direction.

## Projects

Listed bottom-up, each with what it references.

- `Results` — `Result<T>` and `Error`. References nothing.
- `Values` — `IValue`, `IParse`, `ITryParse`. References `Results`.
- `Kingo` — the shared kernel, holding the identifier types. References `Results` and `Values`.
- `Kingo.Facts` — the fact side: the `Fact` root, `Resource`, `SubjectSet`. References `Kingo`, `Results`, `Values`.
- `Kingo.Theories` — the theory side: the `Theory` root, `Namespace`, `Relation`, `SubjectSetRewrite`. References `Kingo` and `Results`.
- `Kingo.Closures` — the interpreters over both halves, carrying `Decision` and `Expansion`. References `Kingo`, `Kingo.Facts`, `Kingo.Theories`, `Results`.
- `Kingo.Documents` — the theory document codec. References `Kingo.Theories` and `Results`.
- `Kingo.Serialization.Json` — a converter pack for the `Kingo` value types. References `Kingo`.

## Domain core

`Kingo` is the shared kernel and holds only identifiers, because aggregates reference each other by identity and identifiers are the currency they share.

The model splits in two. `Kingo.Theories` carries the intensional half: relations define subjects from other subjects, and `SubjectSetRewrite` is the algebra they are written in. It is parse-agnostic and deliberately not an AST. `Kingo.Facts` carries the extensional half: memberships recorded outright, with `Resource` and `SubjectSet` as value objects of that side and the party seated as a `SubjectId`.

Neither half references the other. They meet in `Kingo.Closures`, where the interpreters read facts through a theory.

The core knows nothing about how anything is persisted, serialized, transported, rendered, or authenticated. Where `Parse` may live is settled in [[parse-belongs-to-single-primitives-with-a-grammar]].

## Ports

A port says what the core needs from the outside without saying how — something that can store a fact, not a DynamoDB client. No ports project exists yet. The first one arrives with the storage work, which needs a `GraphOperation` vocabulary that fits in neither the core nor an adapter ([[graph-document-is-bulk-dml]], [[storage-versioning-design]]).

## Adapters

Adapters implement the ports against a specific library or platform, and the domain never references them. `Kingo.Documents` is the theory document codec, built on YamlDotNet and Superpower, exposing `TheoryParser.Parse(text) → Result<Theory>` and the `theory.Print()` extension. `Kingo.Serialization.Json` is a converter pack for the `Kingo` value types so a REST host can bind them; no document crosses the wire.

The fact side has no adapter. That document is a bulk-DML changeset rather than a state, so it waits on the first ports project ([[graph-document-is-bulk-dml]]).

## Enforced

These rules run as tests, so the structure is checked rather than described. Each suite runs ArchUnit rules over its own assembly:

- `Kingo.Facts` and `Kingo.Theories` each refuse an assembly reference to the other.
- Every type sits in the namespace its project declares, so `Kingo` stays flat and nothing gets parked in the kernel.
- `IValue` implementers are `readonly record struct`s.
- Concrete classes are sealed, and instance fields are never public.
- An adapter defines no exception types: parse failures surface as `Result` values, and substrate faults propagate as the substrate's own exceptions.

## Principles

- **The domain doesn't know how it's stored.** No serialization attributes, ORM annotations, or framework references on domain types. Mapping happens at the adapter boundary.
- **Ports speak the domain's language.** A port takes and returns domain types, and never leaks `JsonElement` or `YamlNode` inward.
- **Adapters are swappable in principle.** The system runs against any conforming implementation, which shows up most at test time where fakes substitute for the real ones.
- **First slice sets the layer.** When a new layer appears, its structural rules land with it — naming, project shape, ArchUnit rules.
