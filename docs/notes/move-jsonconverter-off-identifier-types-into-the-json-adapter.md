---
type: todo
title: Move JsonConverter off identifier types into the JSON adapter
summary: "The domain half is moot — the fresh-built core carries no serialization attributes; what remains is the IParse-keyed converter registration in Kingo.Serialization.Json. Unblocked 2026-07-14 by the PDL adapter — the next work item."
tags: [note, todo, hexagonal, serialization]
created: 2026-05-13
status: open
priority: medium
effort: low
---

# Move JsonConverter off identifier types into the JSON adapter

## Observation

The pre-reboot identifier types carried `[JsonConverter(typeof(StringConvertible<...>))]` attributes — the domain type declaring *how it is serialized*. That couples domain to serialization, exactly the leak writing-csharp warns against ("the domain doesn't know how it's stored," generalized to "doesn't know how it's serialized").

The fresh-built `Kingo` core carries no serialization attributes; `[JsonConverter]` and `StringConvertible<T>` survived only in the `Kingo.Pdl` quarry, deleted 2026-07-14 with [[dissolve-kingo-pdl-under-hexagonal-layout]]. The Parse boundary rule in [[domain-language]] settles the design: wire-*capability* lives on the type (`IParse`), the wire *format* lives in the adapter's converters. The adapter layer's shape is now set: converters implement or hang off ports from `Kingo.Serialization` (`IDocumentSerializer<T>` is the first), public surface is port implementations only, and `AdapterArchitectureTestsBase` enforces both.

## Next

- Build the generic `IParse`-keyed JSON/YAML converters in `Kingo.Serialization.Json` / `Kingo.Serialization.Yaml` — one converter family for all value types, registered at the `JsonSerializerOptions` level so the adapter owns the mapping. Queued right behind the PDL adapter ([[dissolve-kingo-pdl-under-hexagonal-layout]]); it unblocks the REST hosts.
- Decide whether the adapter exposes a `JsonSerializerOptions` factory (`KingoJsonOptions.Default`) or registers types via attributes-on-the-options.
- ArchUnit rule (with the adapter tests): no serialization attributes on domain types.
