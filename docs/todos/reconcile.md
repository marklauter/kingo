---
title: Reconcile the rewrite-interpreters branch
type: todo
summary: "Verdict on the 26 doc files the rewrite-interpreters branch carried: three specs ported forward and nothing else did. Closed 2026-08-01; section 2's four salvages were all one mistake, treating late-bound names as URNs."
created: 2026-07-25
priority: high
status: closed
---

# Reconcile the rewrite-interpreters branch

No code on that branch — 26 doc files and one stray transcript. The good news: the real design work is orthogonal to the schema wrong-turn, so the split is cleaner than it looks.

## 1. Keep — port forward with vocabulary translation

1.1. **`docs/specs/fact-reader-port.md`** — RECONCILED 2026-07-27 (`5b39c8e`, `1743d6d`). Landed at the same path. The translation was mechanical as predicted; the claims were not. Verified against the code, which turned up two things the branch couldn't have known: a failed fact lookup has no honest `ErrorType` (recorded as an open question — `Gone` covers snapshot-unavailable, but a broken adapter is neither `NotFound` nor `Undefined`, and behind it sits the fork over whether it stays a value at all), and the narrowed overload needed a clause saying a point-lookup miss doesn't settle direct membership. The `ValueTask` shape composes now that `Result<T>.BindAsync` and `ResultAsync` are ValueTask-currency.

1.2. **`docs/specs/evaluation-context.md`** — RECONCILED 2026-07-27 (`71aa825`, `7fa9af0`). This verdict was wrong. It is not a translation: the file is built on schema-as-wall, and the catalog reversal killed that center. A reference resolves against the catalog rather than the theory that wrote it, and the question already names its own theory, so the prepared projection's unit is unsettled and the scope half of routing has nothing left to route on. What survived was extracted into **`docs/specs/closures.md`** — the per-pin assembly, the pre-pinned reader, the Contains-only depth bound, and both open questions (what the projection spans, and the factory's shape). Routing and the dead `[[domain-language]]` guardrail were dropped, not ported. Corrected along the way: there is *one* pin, not two. `TheoryVersion` is derivable from the `Kookie` ([[storage-versioning-design]], 2026-07-20); what moves at different rates is churn, not pins.

1.3. **`docs/specs/fact-documents.md`** — RECONCILED 2026-07-31 (`dad8a6c`). Landed as **`docs/specs/graph-operations.md`**, named for the concept rather than the file: a fact operation is one row, a graph operation is the atomic set of them, and the spec defines the operation with YAML as its projection. The substance ported — atomic batch, one Write transaction, validated on end state, idempotent, no script identity — with the first two marked `scrutinize` because the design record still holds the transaction question open. Two things changed. The notation went to a property per part rather than to qualified names: [[parse-belongs-to-single-primitives-with-a-grammar]] is locked and `Fact.Parse` is gone from `src`, because user-owned ids may carry the notation's own delimiters, so a flat triple has nothing to read it. And 3.4 reverses — see below.

1.4. **`docs/todos/sdl-parse-layer-has-no-input-size-bound.md`** — RECONCILED 2026-08-01. **Discarded, not ported.** This verdict was wrong. The depth half already landed: `RewriteExpressionParser` carries both guards, the paren scan (`WouldOverflowTheParserStack`) and the tree-height walk (`ExceedsMaxDepth`), and [[theories]] documents the bounds. The size half is not a design question. A theory document arrives as a request body, the endpoint bounds it, and rate limiting covers repetition — ordinary host configuration, with no host yet to configure. The error echo was raised as a second finding and dismissed with it (Mark, 2026-08-01): nobody logs a request body, so a full-expression echo threatens neither the log nor the service.

## 2. Salvage a paragraph, then discard the file

2.1. **`docs/decisions/schema-dissolves-into-administration.md`** — RECONCILED 2026-08-01. **Dropped whole; nothing salvaged.** The shared-groups argument does not survive. Its premise was that an org-wide group has no owner that can hold it usefully, and qualification falsifies that in the writing: `org/group:eng#member` names its owner. The argument also turned on reading a referential wall as a barrier that would force a membership copy. A referential wall is scoping — namespaces are scoped by theory, so a bare name never escapes its theory and a cross-theory reference is spelled in full. It never forced a copy, so there was nothing to argue against. A theory **is** a referential wall (Mark, 2026-08-01), and [[specs/catalog]] stands as written.

2.2. **`docs/notes/spec-as-owned-qualifier.md`** — RECONCILED 2026-08-01. **Dropped whole; no todo opened.** The static-reference fork is not a fork. It assumes a reference must name the theory it reaches, and a reference never does: a computed-subjectset is evaluated on the resource in hand, and that resource already carries its namespace and therefore its theory. Qualification would only be needed to evaluate some *other* namespace's relation on a resource, which the algebra never does. So nothing is missing from the rewrite grammar, and `io/file#viewer` as a term answers a question that does not exist.

This file is evidence of how 2.1 arose (Mark, 2026-08-01). The same mistaken assumption — that a theory name has to be baked into a reference — is what produced the schema-dissolution panic in the first place. Dropping the SQL analogies is what made the real shape visible: a theory is a super-namespace *and* the atomic unit of relation-collection mutation, and the theory/fact continuum names itself.

2.3. **`docs/todos/sdl-becomes-a-script-language.md`** — RECONCILED 2026-07-31 (`dad8a6c`). The reason is inlined in [[specs/graph-operations]]: there is no script identity, run history, or series ordering, because that apparatus exists to make non-idempotent scripts safe to re-run and a graph operation is already idempotent. Phrased without naming the tool. Drop the file.

2.4. **`docs/specs/namespace-documents.md`** — RECONCILED 2026-08-01. **Dropped whole; nothing salvaged.** It became [[theories]], which is better: the branch had `&` and `|` sharing precedence, and `&` now binds tighter. Neither salvage survives.

"A rewrite may reference a not-yet-existing target, and Write tolerates it because evaluation fails closed" is a category error (Mark, 2026-08-01). It presupposes the identifier designates something. Relation identifiers declared with rewrites are late bound, not URNs, so there is no referent to be missing — the branch was guarding against a dangling pointer in a language that has no pointers. The one check that does exist ([[theories]]) is a well-formedness rule keeping a theory determinate within its own namespace, not a reference resolving.

The `DELETE` endpoint question is a host question with no host to ask it of, the same shape as 1.4. And [[theories]] already settles what the branch called "removal is not expressible": a write carries the whole document, and omitting a relation removes it.

**Section 2 carries nothing forward.** All four items were one mistake wearing four hats: treating late-bound names as URNs.

## 3. Discard outright

RECONCILED 2026-08-01. All five discarded with the branch; nothing was read forward from any of them.

3.1. `docs/todos/dissolve-schema-into-administration.md` — execution plan for a reversed decision.

3.2. Every glossary diff on the branch.

3.3. The `domain-language.md` and `schema-definition-language.md` edits — you replaced that file with [[ubiquitous-language]].

3.4. The branch's edits to the two shared todos. [[rewrite-interpreters]] is strictly behind — your version is fully translated to Theory/relation and the branch's only additions are a `blocked-by` on the dead todo and a "deliverable is specs, not code" paragraph you may want to lift verbatim. [[graph-document-is-bulk-dml]]'s branch edit rules the vocabulary to `apply`/`drop` to align with a config side that no longer exists; `create`/`touch`/`delete` stands, and that decides the open question in `fact-documents.md`. **Reversed 2026-07-31 (Mark).** That argument was provenance, not merit. A strict `create` contradicts the idempotence ruling, since re-running a document carrying one fails by design, and the three-op set predates that ruling. `apply`/`drop` stands: upsert is the only assert, and asserting against known state becomes the precondition question. The superseded section is marked in [[graph-document-is-bulk-dml]].

3.5. The stray `2026-07-23-*.txt` transcript.

## 4. Untangling mechanically

RECONCILED 2026-08-01.

4.1. Don't merge. Held — nothing was cherry-picked or merged.

4.2. Copy the keepers out of the sibling clone by hand. Done, and the translation turned out not to be the work; verifying each claim against the code and the corpus was.

4.3. ~~Open two todos.~~ Neither is opened. The parse size bound is discarded (1.4) and the static-reference fork dissolved (2.2).

4.4. ~~Fold the three salvaged paragraphs in.~~ Moot. 2.1's paragraph died with its premise, 2.2's was a category error, and 2.3's is inlined in [[specs/graph-operations]].

4.5. `origin/rewrite-interpreters` deleted 2026-08-01 at `25938b7`. The local branch and the sibling clone at `kingo-rewrite-interpreters` both still hold that tip.

## 5. Flagged, not fixed

5.1. [[realign-serialization-projects-around-their-real-consumers]] still calls the config aggregate `Spec` throughout. Out of scope for the reconcile: a rename sweep, not a verdict on the branch.

## What the reconcile carries into the next branch

Three specs — [[specs/fact-reader-port]], [[specs/closures]], [[specs/graph-operations]] — and seven open questions inside them. Ten `scrutinize` blocks mark claims elsewhere in the corpus whose footing moved; each names what superseded it and why. Nothing in this note decides them. The reversal absorbed here is what makes them answerable, and the new interpreter-design branch is where they get answered.
