---
title: Rewrite test helpers are duplicated
summary: "The six rewrite-construction helpers (Rel, Computed, FactTo, Union, Intersection, Exclusion) live verbatim in both Kingo.Schemas.Tests and Kingo.Sdl.Tests TestHelpers; the namespace-parse-invariants branch paid the sync cost twice in one day. Kingo.Testing is the wrong home — every test project references it, so it would drag Kingo.Schemas into Values.Tests and Results.Tests."
tags: [note, todo, tests]
created: 2026-07-21
status: open
priority: low
effort: low
---

# Rewrite test helpers are duplicated

Raised by code review, 2026-07-21. `tests/Kingo.Schemas.Tests/TestHelpers.cs` and `tests/Kingo.Sdl.Tests/TestHelpers.cs` carry the same six rewrite-construction members character for character; the `SubjectSetRewrite.Exclusion.Create` change (bare → `Result`) had to land in both files, twice in one branch.

Homing them in `Kingo.Testing` is ruled out: every test project references it — `Values.Tests` and `Results.Tests` included — and the helpers need `Kingo.Schemas`, a dependency those projects must not inherit. The candidate shapes:

- A shared source file in `Kingo.Schemas.Tests` linked into `Kingo.Sdl.Tests` via `<Compile Include>` — no new project, but the SDL tests import a foreign namespace.
- A `Kingo.Schemas.Testing` project referenced only by the two consumers — clean, at the cost of a project whose whole content is six one-liners.
- Live with the duplication — two files, test code, and the drift is caught by the compiler whenever a factory signature moves.

Project layout is Mark's call; decide before a third consumer copies the six.
