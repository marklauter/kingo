---
title: SDL becomes a script language
summary: "Closed 2026-07-22 without landing: the script language is superseded by per-namespace config pushes (PUT/DELETE with the namespace body as payload) — the flyway machinery served non-idempotent scripts, and the design's documents came out idempotent."
tags: [todo, sdl, schemas, write]
created: 2026-07-21
priority: high
effort: high
status: closed
blocked-by: "[[dissolve-schema-into-administration]]"
---

# SDL becomes a script language

Under [[schema-dissolves-into-administration]], SDL is the only config-mutation surface: the Write API takes documents, never verb endpoints, so every mutation is git-storable and reviewable. That promotes SDL from document shape to script shape (still YAML). Ruled 2026-07-21, during the [[rewrite-interpreters]] clean room; the grammar design is this todo's work.

## Settled semantics the grammar must land

- **Definition documents are the degenerate script.** The envelope-free namespace map (root is the map; `schema:`/`namespaces:` retired) reads as all-upserts: idempotent create-or-replace per named namespace. Absence means nothing; implicit delete is dead ([[drift-prevention-at-the-write-edges]] already refused it).
- **Deletion is explicit grammar** — a `drop` marker of some spelling, never absence, never an API verb. Refused while live facts reference the namespace; removal stays the two-step migration (migrate facts, land the drop).
- **Two idempotency properties, kept distinct.** Per-document: one apply run twice against the same state lands the same state (retry-safe upsert). Series: config state is a fold — `state_n = apply(state_(n-1), script_n)`, ordered; out-of-order is a different fold, and no claim of order-independence is made (flyway makes none either).
- **The changelog is the history table.** Write is the sole writer; each applied script is an append-only changelog entry, each entry a version state on the store timeline, and the [[kookie]] pins the fold's result at any moment. Config replay is replay of the series — the same story facts already have, on the same timeline.
- **Stored state is the parsable declarative shape.** Version states materialize as per-namespace SDL YAML blobs; the authored artifact is the script series that produced them.

## Design questions

- The drop marker's spelling, and its reserved-word interaction with the SDL tokenizer ([[reserved-words-live-with-the-tokenizer]]).
- Whether an operations-document kind exists beside the definition kind, or one grammar carries both.
- Whether a migration file can sequence FML and SDL steps together ([[graph-document-is-bulk-dml]]) — fact migration before a drop wants both in one series.
- Script identity and ordering metadata (flyway's version numbering equivalent) — filename convention, in-document key, or Write-side sequence.

## Resolution

Closed 2026-07-22 without landing the grammar. The script shape was designed to a working spelling: kind-discriminated documents, `apply`/`drop` blocks, unordered atomic batches. The exercise exposed its own premise. The flyway apparatus (script identity, run history, series ordering) exists to make non-idempotent scripts safe, but the documents had come out idempotent upserts with no-op drops, so the apparatus had nothing to protect. The 2026-07-21 documents-only ruling is superseded: config mutation is per-namespace pushes. `PUT /namespaces/{name}` takes the namespace body (the relationship list) as payload; `DELETE /namespaces/{name}` is refused while live facts reference the namespace. Git-storability survives as one `<namespace>.yaml` per namespace whose wire body is the stored artifact. Details and remaining execution work live in [[dissolve-schema-into-administration]]. The design questions above retire with the script language: no drop marker, no operations-document kind, no script identity. The fact-side document survives on its own track — [[graph-document-is-bulk-dml]] — with `apply`/`drop` as its operation vocabulary (ruled 2026-07-22).
