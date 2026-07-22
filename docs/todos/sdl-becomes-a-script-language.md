---
title: SDL becomes a script language
summary: "Design SDL's script shape: envelope-free definition documents as the degenerate all-upsert script, explicit drop grammar, ordered flyway-style series folded through the changelog, per-namespace SDL blobs as the stored version states."
tags: [todo, sdl, schemas, write]
created: 2026-07-21
priority: high
effort: high
status: open
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
