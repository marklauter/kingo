---
title: graph-operations
type: specification
summary: "A graph operation is an atomic set of fact operations — apply and drop — applied to the graph as one Write transaction. Projected to and from YAML as its wire form, with a property per part of a fact."
tags: [graphs, documents]
created: 2026-07-31
status: evolving
cites:
  - "[[graph]]"
  - "[[fact]]"
  - "[[subjectset]]"
  - "[[subject]]"
  - "[[resource]]"
  - "[[kookie]]"
  - "[[facts]]"
  - "[[theories]]"
  - "[[identifiers]]"
  - "[[parse-belongs-to-single-primitives-with-a-grammar]]"
  - "[[graph-document-is-bulk-dml]]"
  - "[[the-sub-graph-document-is-a-calculated-export]]"
---

# Graph operations

A graph operation is an atomic set of fact operations applied to the graph. A fact operation is one apply or drop naming one fact. The theory document is the model half, declaring the relations facts may assert; this is the data half, mutating the facts those relations range over.

A graph operation is an entity of the model. It enters the system as a YAML document and is applied by the Write service, but the YAML is a projection of the operation, not the operation itself.

## The two fact operations

- **apply** — assert the fact, succeed whether or not it was already there. Always an upsert.
- **drop** — retract the fact. In storage this stamps a tombstone closing the fact's interval rather than removing a row, which leaves the operation vocabulary unchanged.

There is no patch. A fact's domain key is the whole triple, so no non-key field is left to change and a patched fact is a different fact.

There is no strict assert. An operation that conflicts when the fact already exists contradicts the idempotence below: re-running the document would fail by design. Asserting against known state is a precondition question, open in the design record, and a precondition generalizes where a second verb differing only by an if-absent condition does not.

## The set is the unit

<!--scrutinize: the branch ruled this while a schema was a referential wall; graph-document-is-bulk-dml.md still carries "Is a document a transaction?" as an open question, and the rollback shape below is a guess (Mark, 2026-07-31), not a decision.-->
The whole set applies or none of it does: one Write transaction, validated on its end state. Ordering exists between graph operations, never within one, so a document cannot say "drop X, then apply X" — the end state decides. Whether a failed operation rolls back or unwinds some other way is unsettled.

The set is idempotent: applying it twice lands the same state. Apply carries this by being an upsert; on the drop side it rests on drop-of-absent being a no-op, which is open in the design record.
<!--/scrutinize-->

There is no script identity, run history, or series ordering. That apparatus exists to make non-idempotent scripts safe to re-run, and a graph operation is already idempotent, so it would protect nothing. Each application versions on the store timeline, and the kookie pins the fact state any evaluation reads.

## Projection

A fact projects as a property per part rather than as the `io/doc:readme#viewer@10` notation. The ids inside a fact are the user's, opaque, and may carry the notation's own delimiters, so no unambiguous text form exists to parse, and the composites hold no `Parse` for that reason. The notation in the fact spec stays notation for reading. The document decomposes to the leaves, where YAML's own quoting carries an id verbatim.

```yaml
apply:
  - resource:
      namespace: io/doc
      id: "readme#v2"
    relation: viewer
    subject: "10"

  - resource:
      namespace: io/group
      id: eng
    relation: member
    subject: "user:dave"

drop:
  - resource:
      namespace: io/doc
      id: readme
    relation: viewer
    subject: "user:carol"
```

Sections keyed by operation fit a bulk loader, where the common document is four hundred facts to apply and a per-entry operation tag would be noise on every line.

This shape is a first sketch. It is verbose against a batch that size, and reducing that cost is open work.

## Open questions

The design record and the storage-side questions live in the bulk-DML todo: drop of an absent fact, preconditions, drop by filter, and where the operation type lands. Open to this document:

- **How a subject's shape is discriminated.** A subject is a subject id, a subjectset, or a resource member. The flat notation told them apart by punctuation, which the structured form gives up, so the projection needs a discriminator or a distinct property per shape.
- **How the verbosity is paid down** at bulk scale without reintroducing a text form.
- **Whether the sections need a root key.** The branch carried `facts:` above them; nothing discriminates document kinds now, so it may have no job left.
- **Whether a fact appearing in two sections is a parse defect** or is resolved by the end-state rule above.

The read-side counterpart is a different artifact and does not belong here.
