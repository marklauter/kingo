---
title: ResourceFact — a third Fact case replaces the ... sentinel
summary: "Ruled 2026-07-21: `...` is not a relationship. The exactly-specified tupleset member `folder:y#...` is the resource itself, carried by a third Fact case — Fact.ResourceFact, resource-only — and RelationshipIdentifier loses the Nothing sentinel. The three tupleset member shapes become the three Fact cases."
tags: [note, todo, graphs, domain]
created: 2026-07-21
status: closed
priority: high
effort: medium
supports: "[[rewrite-interpreters]]"
---

# ResourceFact — a third Fact case replaces the ... sentinel

Ruled (Mark, 2026-07-21): `...` is not a relationship. The exactly-specified tupleset member `folder:y#...` is not a `SubjectSet` wearing a sentinel — it is the resource itself, and `Fact` gains a third case to carry it: **`Fact.ResourceFact`**, member typed `Resource`, resource-only. `RelationshipIdentifier` loses `Nothing` and its grammar collapses to name-only. The three tupleset member shapes become the three `Fact` cases — exactly-specified = `ResourceFact`, over-specified = `SubjectSetFact`, under-specified = `SubjectFact` — so the traversal's shape analysis is case-matching on the union, not sentinel-peeking inside a `SubjectSet`. The `#...` text stays as the production's punctuation: bare `folder:y` is a legal `<subject-id>` (`:` is in its grammar), so the marker is what keeps `ResourceFact` and `SubjectFact` distinguishable in canonical text.

Consequences settled with the ruling:

- The candidate fourth check of [[namespace-create-validation]] (refuse defining a relationship named `...`) is moot before it was written: a relationship named `...` is unrepresentable once the identifier grammar drops the sentinel, and a dangling rewrite reference to `...` likewise.
- Dry-run finding F18 inverts: `Contains(folder:y#...@user:anne)` is unrepresentable — `...` fails `RelationshipIdentifier.Parse`, so the question dies at the parse edge and never reaches condition 2's schema lookup. The named test pins the parse refusal instead.
- SDL: `RewriteExpressionPrinter.IsReserved` keeps only `this`; the `...` arm dies with the sentinel (`src/Kingo.Sdl/RewriteExpressionPrinter.cs:20`). Confirmed 2026-07-21: `...` is fact-language only — the SDL rewrite grammar cannot lex it, and the shared terminal's accidental acceptance of `...` as a relationship name closes with the regex change.
- A `ResourceFact` member under a rewrite consuming `this` is a modeled data defect — condition 9 in [[rewrite-interpreters]]'s family 2 (ruled Mark, 2026-07-21): the mirror of under-specified, refused upstream by the Write member-shape guard, never skipped, never identity-matched. The paper assigns `#...` no meaning outside tupleset consumption, and Kingo's Subject/Resource split makes identity-matching a category error; a resource meant as a direct member is spelled as a subject-id (`doc:x#viewer@folder:y`, a legal `SubjectFact`).

## Open

Nothing. The seat is **`Subject`** in every case (ruled Mark, 2026-07-21, resolving the day's earlier candidates): "subject" names the *seat* — the paper's ⟨user⟩, a term deriving a set of subjects — and the party is `SubjectIdentifier`. The `Subject` wrapper record was the actual confusion: `DirectSubject` residue from the DU dissolved 2026-07-19, wrapping the identifier and asserting a type the domain denies — subjects exist only as identifiers inside facts. Delete `Subject.cs` and its tests and the seat name stops lying. The earlier objections dissolve with the class: no "three kinds of subjects" (no type claims the seat's name), no `SubjectSetFact` collision (seat `Subject`, left seat `SubjectSet`). The grammar was right as written — `<subject> ::= <subject-id> | <subjectset> | <resource> '#...'` — zero domain-language change. The cases:

- `Fact.SubjectFact(SubjectSet SubjectSet, SubjectIdentifier Subject)`
- `Fact.SubjectSetFact(SubjectSet SubjectSet, SubjectSet Subject)`
- `Fact.ResourceFact(SubjectSet SubjectSet, Resource Subject)`

`Contains` amends with it: `Contains(SubjectSet, SubjectIdentifier)` — the narrowing ruling's substance is unchanged.

## Tasks

Code (`Kingo`, `Kingo.Graphs`, `Kingo.Sdl`):

- [x] `Fact.ResourceFact(SubjectSet SubjectSet, Resource Subject)` — third case; `Fact.Parse` dispatches a member ending in `#...` to it; `ToString` emits the `#...` form.
- [x] Delete `Subject.cs` and its tests; `SubjectFact`'s seat becomes `SubjectIdentifier Subject`, and every consumer follows — `Contains(SubjectSet, SubjectIdentifier)` included.
- [x] `RelationshipIdentifier`: delete `Nothing`; the regex drops the `...` alternative; parsing `...` fails as `relationship_id.invalid`.
- [x] `RewriteExpressionPrinter.IsReserved` reduces to `this`.
- [x] Sweep `Kingo.Sdl` for any other `...` handling (tokenizer, parser).
- [x] Sweep the solution for `Fact` consumers — every match over the union gains the third case. Switch expressions fail the build when non-exhaustive (`TreatWarningsAsErrors`); statement-form switches and `is`-chains need the manual pass.
- [x] Tests: the `RelationshipIdentifierTests` sentinel tests become refusal tests; `Fact.Parse` covers all three cases and round-trips `#...`; a named test pins the `...`-question parse refusal (supersedes the F18 condition-2 test).

Docs:

- [x] [[rewrite-interpreters]]: condition 2 inverted, tupleset shapes and conditions 5–6 restated in case vocabulary, third case added to Naming, blocked-by edge added (2026-07-21).
- [x] [[domain-language]]: `<subject>` production gains `<resource> '#...'`; the `<relationship>` terminal drops `...`; the Fact mapping row carries the third case (2026-07-21).
- [x] The findings ledger: F18 annotated as inverted (2026-07-21; the ledger is since retired from the corpus).
- [x] [[fact]] glossary: the member is a subject, a subject set, or a resource (2026-07-21).
- [x] Rule the `this`-expansion question — condition 9, folded into [[rewrite-interpreters]] (2026-07-21).
- [x] Seat name resolved: `Subject` in every case; the `Subject` class dissolved instead of the word changing, and the grammar stands as written (2026-07-21).
- [x] Live docs swept for `Subject`-as-class references: [[domain-language]] (mapping row, case signatures, value-object list), [[rewrite-interpreters]] (Contains signature, F9, leaf matching, condition 9, Decision seats), [[subject]] glossary (member shapes) (2026-07-21).
- [x] Long-tail doc sweep (2026-07-21): [[architecture]] (value-objects list), [[authz-event-logging]] (Decision payload), [[graph-document-is-bulk-dml]] (Graphs contents line) updated; [[sources]] and [[dissolve-kingo-pdl-under-hexagonal-layout]] stand as history.

## Resolution

Landed in commit `f23f9c8` (branch `reboot`, 2026-07-21): `Fact.ResourceFact` added and `Fact.Parse` dispatches the `#...` member (`src/Kingo.Graphs/Fact.cs`); `src/Kingo.Graphs/Subject.cs` deleted, `SubjectFact` seats `SubjectIdentifier Subject`; `RelationshipIdentifier` name-only (`src/Kingo/RelationshipIdentifier.cs`); `IsReserved` reduced to `this` (`src/Kingo.Sdl/RewriteExpressionPrinter.cs`); tests moved (`FactTests`, new `SubjectSetTests` with the F18-superseding named test, `RelationshipIdentifierTests` refusal tests, four SDL test files). Build gate green, 100% line and branch. Independent code review kept four findings: three applied in the same commit (tupleset reserved coverage restored, `SubjectSet.Parse` hoisted, duplicate `...` theory row dropped), one deferred to [[reserved-words-live-with-the-tokenizer]]. Doc amendments across [[rewrite-interpreters]], [[domain-language]], the findings ledger (since retired), and the glossary landed with the rulings through the day; the "not yet in code" markers were swept when the code landed.
