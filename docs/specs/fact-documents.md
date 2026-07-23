---
title: fact documents
summary: "The mutation document Kingo's Write endpoint accepts: apply and drop blocks of fact triples in the core's canonical text form — the DML side, an unordered atomic batch versioned by the store timeline."
tags: [spec, documents]
created: 2026-07-22
status: evolving
cites:
  - "[[fact]]"
  - "[[factset]]"
---

# Fact documents

A fact document mutates facts — the DML side of Kingo's document formats. It is an **unordered atomic batch**: one Write transaction, validated on its end state, and **idempotent** — applying it twice lands the same state (on the fact side this hangs on drop-of-absent being a no-op, open in [[graph-document-is-bulk-dml]]); ordering exists only between documents. There is no script identity, run history, or version metadata ([[sdl-becomes-a-script-language]] records why the flyway-style alternative lost); each push versions on the store timeline, and the [[kookie]] pins the fact state any evaluation reads.

It is bulk DML: `apply` and `drop` blocks, each holding [[fact]] triples in the canonical text form the core owns (`resource#relationship@member`). `apply` is upsert; `drop` retracts.

```yaml
facts:
  apply:
    - doc:readme#owner@10
    - doc:readme#viewer@group:eng#member
    - folder:a#parent@folder:root#...
  drop:
    - doc:readme#viewer@user:carol
```

The adapter owns only the envelope; the fact grammar stays in core ([[domain-language]]'s Parse boundary rule). Design record and open questions — strict create beside `apply`, drop of an absent fact, preconditions, delete-by-filter — live in [[graph-document-is-bulk-dml]].

## Open

- Whether the `facts:` root key stays now that no document-kind discrimination is needed — with the drop-of-absent, strict-create, precondition, and delete-by-filter questions in [[graph-document-is-bulk-dml]].

## Related

- [[namespace-documents]] — the DDL side: the document that declares which edges a fact document may assert.
- [[graph-document-is-bulk-dml]] — this document's design record.
- [[domain-language]] — the fact grammar and the Parse boundary rule.
