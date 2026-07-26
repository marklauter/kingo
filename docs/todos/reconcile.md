---
title: Reconcile the rewrite-interpreters branch
summary: "Verdict on the 26 doc files the rewrite-interpreters branch carries: four specs and one todo port forward, four files give up a paragraph and go, the rest is casualty of the reversed schema dissolution."
tags: [todo, corpus, acl]
created: 2026-07-25
priority: high
effort: medium
status: open
---

# Reconcile the rewrite-interpreters branch

No code on that branch — 26 doc files and one stray transcript. The good news: the real design work is orthogonal to the schema wrong-turn, so the split is cleaner than it looks.

## 1. Keep — port forward with vocabulary translation

1.1. **`docs/specs/fact-reader-port.md`** — the highest-value artifact on the branch and almost untouched by the wrong turn. `IFactReader`, the two overloads (point lookup / range read), absence-is-the-empty-set with the `Bind`-short-circuit argument, failure meaning only "snapshot couldn't be consulted," the Kleene argument for translating exceptions *at the port*, and the `ValueTask` rationale. Nothing in the current corpus covers this. Translation is mechanical: `subject-set` → `subjectset`, `relationship` → `relation`.

1.2. **`docs/specs/evaluation-context.md`** — survives better than you'd expect, because it was written *before* the dissolution. It assumes two pins moving at different rates (config version on admin action, fact snapshot per request), which is exactly the `(Kookie, TheoryVersion)` pair your current [[rewrite-interpreters]] still holds. Rename `PreparedSchema`→`PreparedTheory`, `ISchemaReader`→`ITheoryReader`, and the prepared-projection argument, the port shape, `Closure` as the unit of execution, and "routing happens before construction" all land. The open question about `ClosureFactory`'s shape is still genuinely open.

1.3. **`docs/specs/fact-documents.md`** — the write-side DML document. Your corpus has [[facts]] (the model) but nothing on how facts get mutated. Keep the substance: unordered atomic batch, one Write transaction, validated on end state, idempotent, no script identity. Update the notation to qualified names (`io/doc:readme#viewer@10`) and reconcile the operation vocabulary — see 3.4.

1.4. **`docs/todos/sdl-parse-layer-has-no-input-size-bound.md`** — a real finding from an adversarial review, entirely independent of the schema question. Depth is bounded exactly; total input size isn't, and error messages echo the full expression. Still true. Retitle off "SDL."

## 2. Salvage a paragraph, then discard the file

2.1. **`docs/decisions/schema-dissolves-into-administration.md`** — the decision is reversed, but its *shared-groups argument* is load-bearing and survived the reversal: an org-wide `group:eng#member` has no owner that can hold it usefully, so every wall would force a membership copy and resurrect group-sync drift. That's the argument for why a theory is not a referential wall. [[catalog]] asserts this ("a rewrite in one theory may reference a relation in another") but doesn't argue it. Move the argument into [[catalog]] or a decision record; drop the file.

2.2. **`docs/notes/spec-as-owned-qualifier.md`** — mostly superseded, but it holds one thing your corpus doesn't settle: **the static-reference fork.** Today's rewrite grammar has no cross-theory reference at all — computed-subjectset is a bare name in the same namespace, the factset half likewise, and the computed half resolves dynamically wherever the walked-to facts land. So "references resolve against the catalog" currently has nothing static to resolve. Either you add qualified references to the rewrite grammar (`io/file#viewer` as a term) or cross-theory coupling stays entirely fact-driven. You leaned toward adding them. That belongs in a todo.

2.3. **`docs/todos/sdl-becomes-a-script-language.md`** — closed without landing, but the resolution paragraph is a real finding worth one line somewhere: the flyway apparatus (script identity, run history, series ordering) exists to make non-idempotent scripts safe, and the documents came out idempotent upserts, so it protected nothing. `fact-documents.md` already cites it; inline the reason and drop the file.

2.4. **`docs/specs/namespace-documents.md`** — superseded by [[theories]], which is better (it fixed the precedence bug: the branch has `&` and `|` sharing precedence; you now correctly bind `&` tighter). Two salvages only: the ruling that a rewrite may reference a not-yet-existing target and Write tolerates it because evaluation fails closed, and the `DELETE` endpoint question. Note that [[theories]] reverses the branch's "removal is not expressible" — omitting a relation now removes it.

## 3. Discard outright

3.1. `docs/todos/dissolve-schema-into-administration.md` — execution plan for a reversed decision.

3.2. Every glossary diff on the branch.

3.3. The `domain-language.md` and `schema-definition-language.md` edits — you replaced that file with [[ubiquitous-language]].

3.4. The branch's edits to the two shared todos. [[rewrite-interpreters]] is strictly behind — your version is fully translated to Theory/relation and the branch's only additions are a `blocked-by` on the dead todo and a "deliverable is specs, not code" paragraph you may want to lift verbatim. [[graph-document-is-bulk-dml]]'s branch edit rules the vocabulary to `apply`/`drop` to align with a config side that no longer exists; `create`/`touch`/`delete` stands, and that decides the open question in `fact-documents.md`.

3.5. The stray `2026-07-23-*.txt` transcript.

## 4. Untangling mechanically

4.1. Don't merge. Cherry-picking anything drags stale vocabulary in.

4.2. Copy the four keepers out of the sibling clone into `reconcile` by hand, then translate.

4.3. Open two todos: the static-reference fork (2.2) and the parse size bound (1.4).

4.4. Fold the three salvaged paragraphs into [[catalog]] and `fact-documents.md`.

4.5. The branch then has nothing left worth keeping and can be deleted.

## 5. Flagged, not fixed

5.1. [[realign-serialization-projects-around-their-real-consumers]] still calls the config aggregate `Spec` throughout.
