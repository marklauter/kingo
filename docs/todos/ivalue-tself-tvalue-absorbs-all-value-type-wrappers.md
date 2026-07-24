---
title: IValue<TSelf, TValue> absorbs all value-type wrappers
summary: "Closed by fresh construction: every identifier in Kingo core implements IValue<TSelf, string>; IStringConvertible survives only in the Kingo.Pdl quarry and dies with it."
tags: [note, todo, hexagonal]
created: 2026-05-13
status: closed
---

# IValue<TSelf, TValue> absorbs all value-type wrappers

Rewrite every value-type wrapper to implement `IValue<TSelf, TValue>`, retire `IStringConvertible<T>`, and drop the throw-on-`Empty` smell.

## Observation

The pre-reboot value-type wrappers (`NamespaceIdentifier`, `RelationIdentifier` in `Kingo.Pdl`) implemented `IStringConvertible<T>` with `static abstract T From(string)` and `static abstract T Empty()` — the latter implemented as a throw because no valid empty value exists.

`IValue<TSelf, TValue>` (in `Values`) is the canonical contract for value-type wrappers:

- `TValue Value { get; }` — public access to the wrapped primitive.
- `static abstract TSelf Unchecked(TValue)` — trusted path, no validation.
- `static abstract Result<TSelf> Parse(string)` — untrusted path, full validation, returns `Result<TSelf>`.
- Inherits `IComparable<TSelf>`, `IEquatable<TSelf>`, `IComparisonOperators<TSelf, TSelf, bool>`.

## Interpretation

Every value-type wrapper in Kingo implements `IValue<TSelf, TValue>`. The contract is strictly richer (carries trust-path semantics, brings full comparison surface) and drops the throw-on-`Empty` smell — types without a meaningful zero simply don't gain one. If a wrapper ever has a meaningful zero (e.g., a numeric ID with `Zero = 0`), that's a separate `IZero<TSelf>` interface layered on top of `IValue`, never a throwing default on `IValue` itself.

## Resolution

Closed 2026-07-14 — the slice landed as fresh construction rather than migration. `src/Kingo` was built new per [[ubiquitous-language]]: all four identifiers (`NamespaceIdentifier`, `RelationshipIdentifier`, `ResourceIdentifier`, `SubjectIdentifier`) implement `IValue<TSelf, string>`. Per the Parse boundary rule in [[ubiquitous-language]], `ITryParse<TSelf>` is a separate opt-in for types crossing the ASP.NET binding boundary and nothing in core declares it — so the planned `ValueParser.TryParse` one-line-delegation migration became moot. `IStringConvertible<T>` and `StringConvertible<T>` survive only inside the dead `Kingo.Pdl` quarry project and are deleted with [[dissolve-kingo-pdl-under-hexagonal-layout]]. The residual enforcement work — an ArchUnitNET rule that every public domain value type implements `IValue<TSelf, TValue>` — remains open; it belongs in `Kingo.Tests`' architecture suite.
