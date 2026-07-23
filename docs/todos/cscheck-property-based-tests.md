---
title: CsCheck property-based tests for parsers and value types
summary: "Refactor to CsCheck properties where the contract is a law over an input space (identifier grammars, Parse/ToString and SDL round-trips, Result algebra), keeping the example-based tests as readable contract documentation."
tags: [note, todo, testing]
created: 2026-07-21
status: open
priority: low
effort: medium
---

# CsCheck property-based tests for parsers and value types

One line tests the whole class: a property quantifies over the input space where a hand-picked example pins one point. The suites whose contracts are laws — accept iff the grammar matches, print then parse is identity — currently pin a handful of chosen points (`"a."`, `"a:"`, trailing delimiters added 2026-07-21). CsCheck generates the points, shrinks failures to minimal counterexamples, and prints the seed that reproduces the failure exactly, so randomization keeps the flaky-is-a-defect rule: every failure is replayable.

## Targets

- **Identifier grammars** (`tests/Kingo.Tests`, five identifier types): generate strings over and around the grammar alphabets; assert `Parse` accepts iff the pattern matches, rejects anything carrying a reserved delimiter (`:`, `#`, `@`) in a reserved position, and that `Unchecked`/`Parse` agree on accepted input.
- **Graphs round-trips** (`tests/Kingo.Graphs.Tests`): `Parse(x.ToString()) == x` for `Resource`, `SubjectSet`, `Fact` over generated valid values.
- **SDL round-trips** (`tests/Kingo.Sdl.Tests`): generate specs, assert print→parse identity — the existing `SpecRoundTripTests` enumerate cases by hand; a generator walks the space between them.
- **Result algebra** (`tests/Results.Tests`): `ResultLawTests` pin functor/applicative/monad laws at fixed points; quantify them over generated values and functions.

## Constraints

- Example-based tests stay as readable contract documentation; properties add coverage beside them.
- CsCheck lands in `Directory.Packages.props` once (central package management).
- The first property suite sets the pattern. The generator design (one shared `Gen` per identifier grammar, reused across projects or duplicated per project) is that slice's decision.
