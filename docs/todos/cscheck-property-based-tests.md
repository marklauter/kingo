---
title: CsCheck property-based tests for parsers and value types
summary: "Refactor to CsCheck properties where the contract is a law over an input space (identifier grammars, Parse/ToString and theory-document round-trips), keeping the example-based tests as readable contract documentation."
tags: [note, todo, testing]
created: 2026-07-21
status: open
priority: low
effort: medium
---

# CsCheck property-based tests for parsers and value types

One line tests the whole class: a property quantifies over the input space where a hand-picked example pins one point. The suites whose contracts are laws — accept iff the grammar matches, print then parse is identity — currently pin a handful of chosen points (`"a."`, `"a:"`, trailing delimiters added 2026-07-21). CsCheck generates the points, shrinks failures to minimal counterexamples, and prints the seed that reproduces the failure exactly, so randomization keeps the flaky-is-a-defect rule: every failure is replayable.

## Targets

<!--scrutinize: stale against decision: parse-belongs-to-single-primitives-with-a-grammar.md — there are no reserved delimiters left to refuse (IdentifierGrammar.IdPattern admits any non-whitespace run), and Resource, SubjectSet, and Fact have no Parse or ToString to round-trip. The first target survives only for the Kingo-owned names; the second has no subject.-->
- **Identifier grammars** (`tests/Kingo.Tests`, five identifier types): generate strings over and around the grammar alphabets; assert `Parse` accepts iff the pattern matches, rejects anything carrying a reserved delimiter (`:`, `#`, `@`) in a reserved position, and that `Unchecked`/`Parse` agree on accepted input.
- **Graphs round-trips** (`tests/Kingo.Facts.Tests`): `Parse(x.ToString()) == x` for `Resource`, `SubjectSet`, `Fact` over generated valid values.
<!--/scrutinize-->
- **Theory-document round-trips** (`tests/Kingo.Documents.Tests`): generate theories, assert print→parse identity — the existing `TheoryRoundTripTests` enumerate cases by hand; a generator walks the space between them.

## Constraints

- Example-based tests stay as readable contract documentation; properties add coverage beside them.
- CsCheck lands in `Directory.Packages.props` once (central package management).
- The first property suite sets the pattern. The generator design (one shared `Gen` per identifier grammar, reused across projects or duplicated per project) is that slice's decision.
