---
title: IParse-keyed converters for the JSON and YAML adapters
summary: "Build the generic IParse-keyed converter family in Kingo.Serialization.Json / .Yaml when the first consumer arrives — the REST hosts or any wire format touching value types. Deferred: both adapter projects are empty shells today, and no domain type carries a serialization attribute."
tags: [note, todo, hexagonal, serialization]
created: 2026-05-13
status: deferred
priority: medium
effort: low
---

# IParse-keyed converters for the JSON and YAML adapters

Retitled 2026-07-21; the original title ("Move JsonConverter off identifier types into the JSON adapter") named a leak that no longer exists, and the filename keeps the old slug so links resolve.

## Observation

The pre-reboot identifier types carried `[JsonConverter(typeof(StringConvertible<...>))]` attributes — the domain declaring how it is serialized. That half is dead: the fresh-built core carries no serialization attributes, and the last remnants were deleted with the `Kingo.Pdl` quarry (2026-07-14, [[dissolve-kingo-pdl-under-hexagonal-layout]]). As of 2026-07-21 both `Kingo.Serialization.Json` and `Kingo.Serialization.Yaml` hold only a `GlobalSuppressions.cs`, and their test projects only the ArchUnit architecture tests.

What remains is constructive, and the design is settled by the Parse boundary rule in [[ubiquitous-language]]: wire-*capability* lives on the type (`IParse`), the wire *format* lives in the adapter's converters. The adapter layer's shape ([[realign-serialization-projects-around-their-real-consumers]]): the converter packs ARE the point of the .Json/.Yaml projects — no ports, no document serializers there — and `AdapterArchitectureTestsBase` enforces that adapters define no exception types.

## When the need arises

Un-parked by the first consumer — the REST hosts, or any wire format that touches value types. Then:

- Build the generic `IParse`-keyed converter family — one converter where `T : IParse<T>` writes via `ToString` and reads via `Parse`, so every value type serializes through the round-trip law (`Parse ∘ ToString = id`) without naming its format; mirrored in YAML.
- Decide the registration shape: a `KingoJsonOptions.Default` options factory the hosts consume, or per-type registration on the options.
- ArchUnit rule (with the adapter tests): no serialization attributes on domain types, so the leak cannot return.
