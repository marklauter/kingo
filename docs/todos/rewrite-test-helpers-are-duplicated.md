---
title: Rewrite test helpers are duplicated
summary: "The six rewrite-construction helpers (Rel, Computed, FactTo, Union, Intersection, Exclusion) live verbatim in both Kingo.Theories.Tests and Kingo.Documents.Tests TestHelpers; the namespace-parse-invariants branch paid the sync cost twice in one day. Kingo.Testing is the wrong home: every test project references it, so it would drag Kingo.Theories into Kingo.Tests, which covers the layer below."
tags: [note, todo, tests]
created: 2026-07-21
status: open
priority: low
effort: low
---

# Rewrite test helpers are duplicated

Raised by code review, 2026-07-21. `tests/Kingo.Theories.Tests/TestHelpers.cs` and `tests/Kingo.Documents.Tests/TestHelpers.cs` carry the same six rewrite-construction members character for character; the `SubjectSetRewrite.Exclusion.Create` change (bare → `Result`) had to land in both files, twice in one branch.

Homing them in `Kingo.Testing` is ruled out: every test project references it, and the helpers need `Kingo.Theories`. `Kingo.Tests` covers `src/Kingo`, the layer `Kingo.Theories` sits above, so helpers in `Kingo.Testing` would make a base-layer test project depend on a higher layer. The rationale first named `Values.Tests` and `Results.Tests` as the projects that must not inherit that dependency; both were deleted 2026-07-29 when `Results` and `Values` became the `MSL.Results` and `MSL.ValueTypes` packages ([[architecture]]), and the objection moved to `Kingo.Tests` rather than dying with them. The candidate shapes:

- A shared source file in `Kingo.Theories.Tests` linked into `Kingo.Documents.Tests` via `<Compile Include>` — no new project, but the document tests import a foreign namespace.
- A `Kingo.Theories.Testing` project referenced only by the two consumers — clean, at the cost of a project whose whole content is six one-liners.
- Live with the duplication — two files, test code, and the drift is caught by the compiler whenever a factory signature moves.

Project layout is Mark's call; decide before a third consumer copies the six.
