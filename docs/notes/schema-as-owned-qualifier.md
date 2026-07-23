---
title: Schema as owned qualifier, not a wall
summary: "Reframing from the rewrite-interpreters clean room: schema returns as the owned qualifier segment of a namespace identity (schema/namespace), enforced at the write edge, never a consistency wall. Rules stay globally consistent (strict, a published contract); facts roam across schemas and may dangle. Triggered by finding facts are schema-agnostic — the keystone the schema dissolution rested on."
tags: [note, schemas, evaluation, write]
created: 2026-07-22
status: evolving
revisits: "[[schema-dissolves-into-administration]]"
---

# Schema as owned qualifier, not a wall

State of an in-flight redesign, opened while designing the rewrite interpreter ([[rewrite-interpreters]]) and finding that facts are schema-agnostic. Terminology is unsettled (`schema` vs `spec`, below); this note uses `schema` throughout.

## The keystone

[[schema-dissolves-into-administration]] retired `Schema` as a domain type *because* facts were schema-agnostic — that premise forces globally-unique bare namespace names, which leaves schema owning nothing structural, which is why it "earns no aggregate status." Every link in that chain hangs off the one premise.

Making the namespace identity `<schema>/<namespace>` puts the schema into every fact's namespace, so facts are no longer schema-agnostic. The keystone is gone, and schema returns in a shape the dissolution never weighed: **wall the rules, free the facts.** The boundary it rejected walled facts too; this one does not.

## Settled this session

- A namespace body *defines* a namespace (not declares); definition is total. The write is always an upsert, create-or-replace of the whole namespace. Omitting a relationship removes it.
- Two envelopes, one shape: a single body (name out of band, on the endpoint) and a block (`namespaces:` map). A block is one atomic batch, order-free — names are forward-declared and resolved against the batch plus the store, unresolved is a parse error.
- Schema is the **owned qualifier segment** of the identity: `<schema>/<namespace>`. Schema names are globally unique; namespace names are unique only within their schema; `schema/namespace` is globally unique. Two teams can each own a `documents` namespace. A namespace cannot be in two schemas — the qualifier is part of identity, so `a/documents` and `b/documents` are simply distinct.
- The qualifier is enforced, not convention: the write edge refuses a mutation under a schema the caller does not own. Ownership is expressible in Kingo itself — a claim on the schema is a fact; the write edge does a [[check]] before admitting the mutation. No new domain type.
- References and consistency stay **global**. Schema is naming and ownership only, never a reference wall. A rule in `a/` may name `b/…`; a fact may cross schemas (resource in one, subject in another), which is how a shared group works: `org/group:eng#member` is defined once and referenced everywhere as a subject, no per-schema copy.
- **Facts may dangle freely.** No cascade, no tombstone-gating on delete. Kingo does not "deny" — it computes containership as set algebra; a deleted relation removes a traversal path, so that path contributes the empty set and a stranded fact is on no satisfiable path. An orphan-finder tool is a possible later convenience, not a requirement.
- Deletion is gated on the **rule** consistency of *other* schemas, not on facts. Strict, because a published schema is a public contract others build on and cannot be quietly broken.

## Terminology (open)

`config` is the wrong word for the rule layer. Two candidates:

- `rules` — the Datalog pairing (rules evaluated over facts); names exactly the reference graph that carries the global consistency requirement.
- `spec` — Mark's proposal. Two mappings, undecided:
  - *Reading 1 (low churn):* schema stays the container (`<schema>/<namespace>` unchanged); `spec` is a schema's contents, its namespace collection, the artifact pushed.
  - *Reading 2:* `schema` becomes the whole global rule space (the word replacing `config`); `spec` is the owned container, and the identity re-letters to `<spec>/<namespace>`.
  - Leaning Reading 1. Minor collision either way with the docs corpus `docs/specs/`.

## The open fork — a contract needs something static to bind to

Strict cross-schema consistency can only bite on references that are **static**, and the current rewrite grammar has none crossing a schema. A [[computed-subject-set]] is a bare identifier, same namespace. A [[fact-to-subject-set]]'s first identifier is same namespace. Its second identifier is the only cross-schema reach, and it resolves at evaluation in whatever schema the walked-to facts land in — dynamic, not a static reference to a named schema.

So "strict across the schema" currently has nothing cross-schema to enforce; it collapses to intra-schema integrity (which the atomic push already gives), while every real cross-schema dependency rides on facts, which dangle freely. The fork:

1. **Add static qualified references** to the rule grammar — a term like `drive/documents#viewer`, an explicit import. The dependency becomes visible, checked at publish (target must exist), protected on delete (RESTRICT). The contract gets teeth, but a schema no longer validates in isolation — it needs the schemas it imports.
2. **Keep cross-schema coupling all-dynamic** — no qualified references; cross-schema reach stays fact-driven and evaluation-resolved. Schemas validate independently, but the "unbreakable contract" is soft: a deleted relation silently empties a consumer's paths. "Strict" then honestly means intra-schema only.

Mark leans 1. Unresolved.

## Corpus ripples to reconcile (flagged, not swept)

- [[schema]] — the locked entry ("purely human grouping," "nothing stored under it") no longer holds: the schema is structural, a segment of every identity, and its ownership is stored as facts. Still not a stored aggregate, still not a consistency boundary. Needs re-kernelling.
- [[schema-dissolves-into-administration]] — its "hierarchical organization is naming convention" clause becomes "an enforced schema qualifier"; its retirement of schema-as-type is reopened by the keystone shift above.
- [[namespace-documents]] — its "removal is not expressible" and "cannot delete a namespace a fact uses" are reversed here: facts dangle freely. The doc's framing as a *namespace* document is also in question once the schema is the owned unit.
