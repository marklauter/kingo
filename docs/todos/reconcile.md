---
title: Reconcile the rewrite-interpreters branch
type: todo
summary: "Verdict on the 26 doc files the rewrite-interpreters branch carries: three specs port forward, four files give up a paragraph and go, the rest is casualty of the reversed schema dissolution. Section 1 is closed."
created: 2026-07-25
priority: high
status: open
---

# Reconcile the rewrite-interpreters branch

No code on that branch — 26 doc files and one stray transcript. The good news: the real design work is orthogonal to the schema wrong-turn, so the split is cleaner than it looks.

## 1. Keep — port forward with vocabulary translation

1.1. **`docs/specs/fact-reader-port.md`** — RECONCILED 2026-07-27 (`5b39c8e`, `1743d6d`). Landed at the same path. The translation was mechanical as predicted; the claims were not. Verified against the code, which turned up two things the branch couldn't have known: a failed fact lookup has no honest `ErrorType` (recorded as an open question — `Gone` covers snapshot-unavailable, but a broken adapter is neither `NotFound` nor `Undefined`, and behind it sits the fork over whether it stays a value at all), and the narrowed overload needed a clause saying a point-lookup miss doesn't settle direct membership. The `ValueTask` shape composes now that `Result<T>.BindAsync` and `ResultAsync` are ValueTask-currency.

1.2. **`docs/specs/evaluation-context.md`** — RECONCILED 2026-07-27 (`71aa825`, `7fa9af0`). This verdict was wrong. It is not a translation: the file is built on schema-as-wall, and the catalog reversal killed that center. A reference resolves against the catalog rather than the theory that wrote it, and the question already names its own theory, so the prepared projection's unit is unsettled and the scope half of routing has nothing left to route on. What survived was extracted into **`docs/specs/closures.md`** — the per-pin assembly, the pre-pinned reader, the Contains-only depth bound, and both open questions (what the projection spans, and the factory's shape). Routing and the dead `[[domain-language]]` guardrail were dropped, not ported. Corrected along the way: there is *one* pin, not two. `TheoryVersion` is derivable from the `Kookie` ([[storage-versioning-design]], 2026-07-20); what moves at different rates is churn, not pins.

1.3. **`docs/specs/fact-documents.md`** — RECONCILED 2026-07-31 (`dad8a6c`). Landed as **`docs/specs/graph-operations.md`**, named for the concept rather than the file: a fact operation is one row, a graph operation is the atomic set of them, and the spec defines the operation with YAML as its projection. The substance ported — atomic batch, one Write transaction, validated on end state, idempotent, no script identity — with the first two marked `scrutinize` because the design record still holds the transaction question open. Two things changed. The notation went to a property per part rather than to qualified names: [[parse-belongs-to-single-primitives-with-a-grammar]] is locked and `Fact.Parse` is gone from `src`, because user-owned ids may carry the notation's own delimiters, so a flat triple has nothing to read it. And 3.4 reverses — see below.

1.4. **`docs/todos/sdl-parse-layer-has-no-input-size-bound.md`** — RECONCILED 2026-08-01. **Discarded, not ported.** This verdict was wrong. The depth half already landed: `RewriteExpressionParser` carries both guards, the paren scan (`WouldOverflowTheParserStack`) and the tree-height walk (`ExceedsMaxDepth`), and [[theories]] documents the bounds. The size half is not a design question. A theory document arrives as a request body, the endpoint bounds it, and rate limiting covers repetition — ordinary host configuration, with no host yet to configure. The error echo was raised as a second finding and dismissed with it (Mark, 2026-08-01): nobody logs a request body, so a full-expression echo threatens neither the log nor the service.

## 2. Salvage a paragraph, then discard the file

2.1. **`docs/decisions/schema-dissolves-into-administration.md`** — the decision is reversed, but its *shared-groups argument* is load-bearing and survived the reversal: an org-wide `org/group:eng#member` has no owner that can hold it usefully, so every wall would force a membership copy and resurrect group-sync drift. That's the argument for why a theory is not a referential wall. [[specs/catalog]] asserts this ("a rewrite in one theory may reference a relation in another") but doesn't argue it. Move the argument into [[specs/catalog]] or a decision record; drop the file.

2.2. **`docs/notes/spec-as-owned-qualifier.md`** — mostly superseded, but it holds one thing your corpus doesn't settle: **the static-reference fork.** Today's rewrite grammar has no cross-theory reference at all — computed-subjectset is a bare name in the same namespace, the factset half likewise, and the computed half resolves dynamically wherever the walked-to facts land. So "references resolve against the catalog" currently has nothing static to resolve. Either you add qualified references to the rewrite grammar (`io/file#viewer` as a term) or cross-theory coupling stays entirely fact-driven. You leaned toward adding them. That belongs in a todo.

2.3. **`docs/todos/sdl-becomes-a-script-language.md`** — closed without landing, but the resolution paragraph is a real finding worth one line somewhere: the flyway apparatus (script identity, run history, series ordering) exists to make non-idempotent scripts safe, and the documents came out idempotent upserts, so it protected nothing. `fact-documents.md` already cites it; inline the reason and drop the file.

2.4. **`docs/specs/namespace-documents.md`** — superseded by [[theories]], which is better (it fixed the precedence bug: the branch has `&` and `|` sharing precedence; you now correctly bind `&` tighter). Two salvages only: the ruling that a rewrite may reference a not-yet-existing target and Write tolerates it because evaluation fails closed, and the `DELETE` endpoint question. Note that [[theories]] reverses the branch's "removal is not expressible" — omitting a relation now removes it.

## 3. Discard outright

3.1. `docs/todos/dissolve-schema-into-administration.md` — execution plan for a reversed decision.

3.2. Every glossary diff on the branch.

3.3. The `domain-language.md` and `schema-definition-language.md` edits — you replaced that file with [[ubiquitous-language]].

3.4. The branch's edits to the two shared todos. [[rewrite-interpreters]] is strictly behind — your version is fully translated to Theory/relation and the branch's only additions are a `blocked-by` on the dead todo and a "deliverable is specs, not code" paragraph you may want to lift verbatim. [[graph-document-is-bulk-dml]]'s branch edit rules the vocabulary to `apply`/`drop` to align with a config side that no longer exists; `create`/`touch`/`delete` stands, and that decides the open question in `fact-documents.md`. **Reversed 2026-07-31 (Mark).** That argument was provenance, not merit. A strict `create` contradicts the idempotence ruling, since re-running a document carrying one fails by design, and the three-op set predates that ruling. `apply`/`drop` stands: upsert is the only assert, and asserting against known state becomes the precondition question. The superseded section is marked in [[graph-document-is-bulk-dml]].

3.5. The stray `2026-07-23-*.txt` transcript.

## 4. Untangling mechanically

4.1. Don't merge. Cherry-picking anything drags stale vocabulary in.

4.2. Copy the four keepers out of the sibling clone into `reconcile` by hand, then translate.

4.3. Open one todo: the static-reference fork (2.2). The parse size bound is discarded — see 1.4.

4.4. Fold the three salvaged paragraphs into [[specs/catalog]] and `fact-documents.md`.

4.5. The branch then has nothing left worth keeping and can be deleted.

## 5. Flagged, not fixed

5.1. [[realign-serialization-projects-around-their-real-consumers]] still calls the config aggregate `Spec` throughout.
